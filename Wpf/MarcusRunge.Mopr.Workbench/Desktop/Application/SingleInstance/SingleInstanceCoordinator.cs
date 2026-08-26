using MarcusRunge.Mopr.Workbench.Application.Diagnostics;
using System;
using System.Globalization;
using System.IO.Pipes;
using System.Runtime.ExceptionServices;
using System.Security.AccessControl;
using System.Security.Principal;
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
        private readonly ManualResetEventSlim _markerAcquisitionCompleted = new(false);
        private readonly ManualResetEventSlim _markerReleaseRequested = new(false);
        private Thread? _markerOwnerThread;
        private Exception? _markerAcquisitionException;
        private Task? _serverTask;
        private Func<SingleInstanceRequest, CancellationToken, Task>? _requestHandler;
        private bool _ownsInstanceMarker;
        private bool _acquiredAbandonedMarker;
        private int _acquisitionAttempted;
        private int _disposed;

        public SingleInstanceStartResult TryBecomePrimaryInstance()
        {
            ThrowIfDisposed();

            if (Interlocked.Exchange(ref _acquisitionAttempted, 1) != 0)
            {
                throw new InvalidOperationException("The single-instance marker acquisition has already been attempted.");
            }

            _markerOwnerThread = new Thread(OwnInstanceMarker)
            {
                IsBackground = true,
                Name = "MOPR single-instance marker owner"
            };

            _markerOwnerThread.Start();
            _markerAcquisitionCompleted.Wait();

            if (_markerAcquisitionException is not null)
            {
                ExceptionDispatchInfo.Capture(_markerAcquisitionException).Throw();
            }

            if (_ownsInstanceMarker)
            {
                _diagnostics.WriteInformation(Format(WorkbenchResources.SingleInstancePrimaryInstanceEstablished, Environment.ProcessId));

                if (_acquiredAbandonedMarker)
                {
                    _diagnostics.WriteInformation("The global MOPR single-instance marker was abandoned by the previous owning process and has been recovered.");
                }

                return SingleInstanceStartResult.PrimaryInstance;
            }

            _diagnostics.WriteInformation(Format(WorkbenchResources.SingleInstanceSecondaryInstanceDetected, Environment.ProcessId));
            return SingleInstanceStartResult.SecondaryInstance;
        }

        public void StartListening(Func<SingleInstanceRequest, CancellationToken, Task> requestHandler)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(requestHandler);

            if (!_ownsInstanceMarker)
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

            // The secondary instance normally owns the foreground permission and explicitly transfers it to the known primary process.
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
                    // Canceling the waiting pipe server is an expected part of application shutdown.
                }
            }

            ReleaseInstanceMarker();
            _stopping.Dispose();
            _markerAcquisitionCompleted.Dispose();
            _markerReleaseRequested.Dispose();
        }

        private void OwnInstanceMarker()
        {
            Mutex? instanceMarker = null;
            var ownsMarker = false;

            try
            {
                instanceMarker = OpenOrCreateInstanceMarker();

                try
                {
                    ownsMarker = instanceMarker.WaitOne(0);
                }
                catch (AbandonedMutexException)
                {
                    // Windows transfers ownership to this thread when the previous owner terminated without releasing the mutex.
                    ownsMarker = true;
                    _acquiredAbandonedMarker = true;
                }

                _ownsInstanceMarker = ownsMarker;
                _markerAcquisitionCompleted.Set();

                if (!ownsMarker)
                {
                    return;
                }

                // Mutex ownership is thread-affine, so this thread remains alive until application shutdown requests the release.
                _markerReleaseRequested.Wait();
            }
            catch (Exception exception)
            {
                _markerAcquisitionException = exception;
                _markerAcquisitionCompleted.Set();
            }
            finally
            {
                if (ownsMarker)
                {
                    try
                    {
                        instanceMarker!.ReleaseMutex();
                    }
                    finally
                    {
                        _ownsInstanceMarker = false;
                    }
                }

                instanceMarker?.Dispose();
            }
        }

        private Mutex OpenOrCreateInstanceMarker()
        {
            const MutexRights requiredRights = MutexRights.Synchronize | MutexRights.Modify;

            try
            {
                return MutexAcl.OpenExisting(_options.MutexName, requiredRights);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // The marker does not exist yet. Creation is attempted below.
            }

            var security = CreateInstanceMarkerSecurity();

            try
            {
                return MutexAcl.Create(false, _options.MutexName, out _, security);
            }
            catch (UnauthorizedAccessException)
            {
                // Another user or a concurrent process may have created the marker between the open and create attempts.
                return MutexAcl.OpenExisting(_options.MutexName, requiredRights);
            }
        }

        private static MutexSecurity CreateInstanceMarkerSecurity()
        {
            var authenticatedUsers = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            var security = new MutexSecurity();

            security.AddAccessRule(new MutexAccessRule(authenticatedUsers, MutexRights.Synchronize | MutexRights.Modify, AccessControlType.Allow));

            return security;
        }

        private void ReleaseInstanceMarker()
        {
            var ownerThread = _markerOwnerThread;

            if (ownerThread is null)
            {
                return;
            }

            _markerReleaseRequested.Set();
            ownerThread.Join();
            _markerOwnerThread = null;
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

            await requestHandler(request, cancellationToken).ConfigureAwait(false);
            await SingleInstanceProtocol.WriteAsync(server, new SingleInstanceAcknowledgement(true), cancellationToken).ConfigureAwait(false);

            _diagnostics.WriteInformation(Format(WorkbenchResources.SingleInstanceRequestProcessed, request.Arguments.Length));
        }

        private static string Format(string format, params object?[] arguments) => string.Format(CultureInfo.CurrentCulture, format, arguments);

        private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed != 0, this);
    }
}