using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;

namespace MarcusRunge.Mopr.Workbench.Test.Shared.Application
{
    /// <summary>
    /// Provides a controllable application lifetime for automated tests.
    /// </summary>
    public sealed class TestApplicationLifetime : ILifetimeService, IDisposable
    {
        private readonly CancellationTokenSource _applicationStopping = new();
        private bool _disposed;

        /// <inheritdoc/>
        public CancellationToken ApplicationStopping => _applicationStopping.Token;

        /// <summary>
        /// Signals application shutdown.
        /// </summary>
        public void Cancel()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _applicationStopping.Cancel();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _applicationStopping.Cancel();
            _applicationStopping.Dispose();
        }
    }
}