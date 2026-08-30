using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Contracts.Miras;
using MarcusRunge.Mopr.Workbench.Contracts.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Miras.Models;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Miras;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Implementations.Miras
{
    /// <summary>
    /// Controls the lifetime, concurrency and cancellation of application-level
    /// MIRAS integrity checks.
    /// </summary>
    internal sealed class MirasFlowService : CreateableBindableBase<IMirasFlowService, MirasFlowService, IMirasApplicationServiceBase>, IMirasFlowService
    {
        private readonly object _synchronization = new();

        private IApplicationLifetime? _applicationLifetime;
        private Task<MirasOperationResult>? _activeRun;
        private MirasFlowState _currentState = MirasFlowState.Idle;
        private MirasOperationResult? _lastResult;
        private Exception? _lastUnexpectedError;
        private IMirasService? _mirasService;
        private CancellationTokenSource? _userCancellation;

        /// <inheritdoc/>
        public bool CanCancel
        {
            get
            {
                lock (_synchronization)
                {
                    return _activeRun is not null;
                }
            }
        }

        /// <inheritdoc/>
        public bool CanStart
        {
            get
            {
                lock (_synchronization)
                {
                    return _activeRun is null && !ApplicationLifetime.ApplicationStopping.IsCancellationRequested;
                }
            }
        }

        /// <inheritdoc/>
        public MirasFlowState CurrentState
        {
            get
            {
                lock (_synchronization)
                {
                    return _currentState;
                }
            }
        }

        /// <inheritdoc/>
        public bool HasUnexpectedError
        {
            get
            {
                lock (_synchronization)
                {
                    return _lastUnexpectedError is not null;
                }
            }
        }

        /// <inheritdoc/>
        public bool IsRunning
        {
            get
            {
                lock (_synchronization)
                {
                    return _activeRun is not null;
                }
            }
        }

        /// <inheritdoc/>
        public MirasOperationResult? LastResult
        {
            get
            {
                lock (_synchronization)
                {
                    return _lastResult;
                }
            }
        }

        /// <summary>
        /// Gets the most recent unexpected exception for internal diagnostics.
        /// The exception is deliberately not exposed through the public UI-facing contract.
        /// </summary>
        internal Exception? LastUnexpectedError
        {
            get
            {
                lock (_synchronization)
                {
                    return _lastUnexpectedError;
                }
            }
        }

        private IApplicationLifetime ApplicationLifetime => _applicationLifetime ?? throw new InvalidOperationException("The application lifetime has not been initialized.");

        private IMirasService MirasService => _mirasService ?? throw new InvalidOperationException("The MIRAS check service has not been initialized.");

        /// <summary>
        /// Creates a MIRAS flow service owned by the supplied MIRAS application service.
        /// </summary>
        /// <param name="base">The owning MIRAS application service context.</param>
        /// <returns>The created flow service, or <see langword="null"/> when no context was supplied.</returns>
        internal new static IMirasFlowService? Create(IMirasApplicationServiceBase? @base)
        {
            if (@base is null)
            {
                return null;
            }

            // The flow contains mutable run state and cancellation sources. It must
            // therefore be owned by one Core module instead of being shared globally.
            var service = new MirasFlowService();
            service.OnCreate(@base);

            return service;
        }

        /// <inheritdoc/>
        public void Cancel()
        {
            CancellationTokenSource? userCancellation;

            lock (_synchronization)
            {
                userCancellation = _userCancellation;
            }

            if (userCancellation is null)
            {
                return;
            }

            try
            {
                // Cancellation remains idempotent when completion and user
                // interaction reach the active operation concurrently.
                userCancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The run completed after its cancellation source was captured.
            }
        }

        /// <inheritdoc/>
        public Task<MirasOperationResult> StartAsync(CancellationToken cancellationToken = default)
        {
            Task<MirasOperationResult> activeRun;

            lock (_synchronization)
            {
                if (_activeRun is not null)
                {
                    return _activeRun;
                }

                if (ApplicationLifetime.ApplicationStopping.IsCancellationRequested)
                {
                    return Task.FromCanceled<MirasOperationResult>(ApplicationLifetime.ApplicationStopping);
                }

                cancellationToken.ThrowIfCancellationRequested();

                _userCancellation = new CancellationTokenSource();
                _lastResult = null;
                _lastUnexpectedError = null;
                _currentState = MirasFlowState.Running;

                // RunAsync yields before calling MIRAS so the shared task is assigned
                // before a synchronously completing implementation can finish.
                activeRun = RunAsync(_userCancellation, cancellationToken);
                _activeRun = activeRun;
            }

            RaiseFlowPropertiesChanged();

            return activeRun;
        }

        /// <inheritdoc/>
        protected override void OnCreate(IMirasApplicationServiceBase @base)
        {
            var applicationServiceBase = @base ?? throw new ArgumentNullException(nameof(@base));
            _applicationLifetime = applicationServiceBase.CoreBase.ApplicationLifetime;
            _mirasService = applicationServiceBase.CoreBase.MirasService;
        }

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IMirasApplicationServiceBase @base, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        private async Task<MirasOperationResult> RunAsync(CancellationTokenSource userCancellation, CancellationToken callerCancellation)
        {
            await Task.Yield();

            using var effectiveCancellation = CancellationTokenSource.CreateLinkedTokenSource(userCancellation.Token, callerCancellation, ApplicationLifetime.ApplicationStopping);

            try
            {
                var result = await MirasService.CheckRepositoryAsync(effectiveCancellation.Token).ConfigureAwait(false);

                lock (_synchronization)
                {
                    _lastResult = result;
                    _currentState = MirasFlowState.Completed;
                }

                return result;
            }
            catch (OperationCanceledException) when (effectiveCancellation.IsCancellationRequested)
            {
                lock (_synchronization)
                {
                    _currentState = MirasFlowState.Canceled;
                }

                throw;
            }
            catch (Exception exception)
            {
                lock (_synchronization)
                {
                    _lastUnexpectedError = exception;
                    _currentState = MirasFlowState.Failed;
                }

                throw;
            }
            finally
            {
                CancellationTokenSource? completedUserCancellation;

                lock (_synchronization)
                {
                    completedUserCancellation = _userCancellation;
                    _userCancellation = null;
                    _activeRun = null;
                }

                completedUserCancellation?.Dispose();
                RaiseFlowPropertiesChanged();
            }
        }

        private void RaiseFlowPropertiesChanged()
        {
            RaisePropertyChanged(nameof(CurrentState));
            RaisePropertyChanged(nameof(IsRunning));
            RaisePropertyChanged(nameof(CanStart));
            RaisePropertyChanged(nameof(CanCancel));
            RaisePropertyChanged(nameof(LastResult));
            RaisePropertyChanged(nameof(HasUnexpectedError));
        }
    }
}