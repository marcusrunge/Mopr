using MarcusRunge.Mopr.Workbench.Application.Diagnostics;
using System;
using System.Globalization;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using WorkbenchResources = MarcusRunge.Mopr.Workbench.Properties.Resources;

namespace MarcusRunge.Mopr.Workbench.Application.SingleInstance
{
    internal sealed class SingleInstanceCoordinator(SingleInstanceOptions options, IStartupDiagnostics diagnostics, IForegroundPermission foregroundPermission) : IAsyncDisposable
    {
        private readonly SingleInstanceOptions _options = options ?? throw new ArgumentNullException(nameof(options));
        private readonly IStartupDiagnostics _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        private readonly IForegroundPermission _foregroundPermission = foregroundPermission ?? throw new ArgumentNullException(nameof(foregroundPermission));
        private readonly CancellationTokenSource _stopping = new();
        private Mutex? _instanceMarker;
        private Task? _serverTask;
        private Func<SingleInstanceRequest, CancellationToken, Task>? _requestHandler;
        private int _disposed;

        public SingleInstanceStartResult TryBecomePrimaryInstance()
        {
            ThrowIfDisposed();

            _instanceMarker = new Mutex(false, _options.MutexName, out var createdNew);
            if (createdNew)
            {
                _diagnostics.WriteInformation(Format(WorkbenchResources.SingleInstancePrimaryInstanceEstablished, Environment.ProcessId));
                return SingleInstanceStartResult.PrimaryInstance;
            }

            _instanceMarker.Dispose();
            _instanceMarker = null;
            _diagnostics.WriteInformation(Format(WorkbenchResources.SingleInstanceSecondaryInstanceDetected, Environment.ProcessId));
            return SingleInstanceStartResult.SecondaryInstance;
        }

        public void StartListening(Func<SingleInstanceRequest, CancellationToken, Task> requestHandler)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(requestHandler);

            if (_instanceMarker is null)
            {
                throw new InvalidOperationException(WorkbenchResources.SingleInstancePrimaryInstanceRequiredForServer);
            }

            if (_serverTask is not null)
            {
                throw new InvalidOperationException(WorkbenchResources.SingleInstanceServerAlreadyStarted);
            }

            _requestHandler = requestHandler;
            _serverTask = RunServerAsync(_stopping.Token);
        }

        public async Task ForwardToPrimaryInstanceAsync(string[] arguments, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            using var timeoutSource = new CancellationTokenSource(_options.ClientConnectionTimeout);
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
            var effectiveToken = linkedSource.Token;

            await using var client = new NamedPipeClientStream(".", _options.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

            await client.ConnectAsync(effectiveToken).ConfigureAwait(false);

            var handshake = await SingleInstanceProtocol.ReadAsync<SingleInstanceHandshake>(client, effectiveToken).ConfigureAwait(false);

            // Die gestartete Zweitinstanz besitzt typischerweise das Vordergrundrecht und darf es
            // gezielt an die bekannte primäre Prozess-ID übertragen.
            _foregroundPermission.AllowPrimaryInstance(handshake.PrimaryProcessId);

            await SingleInstanceProtocol.WriteAsync(client, SingleInstanceRequest.Create(arguments), effectiveToken).ConfigureAwait(false);

            var acknowledgement = await SingleInstanceProtocol.ReadAsync<SingleInstanceAcknowledgement>(client, effectiveToken).ConfigureAwait(false);

            if (!acknowledgement.Accepted)
            {
                throw new InvalidOperationException(WorkbenchResources.SingleInstanceRequestRejected);
            }

            _diagnostics.WriteInformation(Format(WorkbenchResources.SingleInstanceRequestForwarded, handshake.PrimaryProcessId));
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _stopping.Cancel();

            if (_serverTask is not null)
            {
                try
                {
                    await _serverTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_stopping.IsCancellationRequested)
                {
                    // Das Beenden des wartenden Pipe-Servers ist regulärer Teil des Shutdowns.
                }
            }

            _instanceMarker?.Dispose();
            _stopping.Dispose();
        }

        private async Task RunServerAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await AcceptSingleRequestAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _diagnostics.WriteError(WorkbenchResources.SingleInstanceRequestProcessingFailed, exception);
                }
            }
        }

        private async Task AcceptSingleRequestAsync(CancellationToken cancellationToken)
        {
            await using var server = new NamedPipeServerStream(_options.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

            await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
            await SingleInstanceProtocol.WriteAsync(server, new SingleInstanceHandshake(Environment.ProcessId), cancellationToken).ConfigureAwait(false);

            var request = await SingleInstanceProtocol.ReadAsync<SingleInstanceRequest>(server, cancellationToken).ConfigureAwait(false);
            var requestHandler = _requestHandler ?? throw new InvalidOperationException(WorkbenchResources.SingleInstanceRequestHandlerMissing);

            await requestHandler(request, cancellationToken).ConfigureAwait(false); await SingleInstanceProtocol.WriteAsync(server, new SingleInstanceAcknowledgement(true), cancellationToken).ConfigureAwait(false);

            _diagnostics.WriteInformation(Format(WorkbenchResources.SingleInstanceRequestProcessed, request.Arguments.Length));
        }

        private static string Format(string format, params object?[] arguments) => string.Format(CultureInfo.CurrentCulture, format, arguments);

        private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed != 0, this);
    }
}