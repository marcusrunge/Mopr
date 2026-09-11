using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Services.Miras.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Implementations
{
    /// <summary>
    /// Controls the lifetime, concurrency and cancellation of application-level
    /// MIRAS integrity checks.
    /// </summary>
    internal sealed class Flow : CreateableBindableBase<IFlow, Flow, IMirasBase>, IFlow
    {
        private readonly Lock _synchronization = new();

        private Task<MirasOperationResult>? _activeRun;        
        private IMirasBase? _base;
        private MirasFlowState _currentState = MirasFlowState.Idle;
        private MirasOperationResult? _lastResult;
        private Exception? _lastUnexpectedError;        
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
                    return _activeRun is null && !_base?.LifetimeService?.ApplicationStopping.IsCancellationRequested == true;
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

                if (_base?.LifetimeService?.ApplicationStopping.IsCancellationRequested == true)
                {
                    return Task.FromCanceled<MirasOperationResult>(_base.LifetimeService.ApplicationStopping);
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

        protected override void OnCreate(IMirasBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

        protected override Task OnCreateAsync(IMirasBase @base, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
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

        private async Task<MirasOperationResult> RunAsync(CancellationTokenSource userCancellation, CancellationToken callerCancellation)
        {
            if(_base?.Operations == null)
            {
                throw new InvalidOperationException("The MIRAS operations are not available.");
            }

            await Task.Yield();

            using var effectiveCancellation = CancellationTokenSource.CreateLinkedTokenSource(userCancellation.Token, callerCancellation, _base?.LifetimeService?.ApplicationStopping ?? CancellationToken.None);

            try
            {
                var result = await _base!.Operations.CheckRepositoryAsync(effectiveCancellation.Token).ConfigureAwait(false);

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
    }
}