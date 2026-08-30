using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using System;
using System.Threading;

namespace MarcusRunge.Mopr.Workbench.Application.Lifetime
{
    internal sealed class ApplicationLifetime : IApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource _applicationStopping = new();
        private int _disposed;
        private int _stopping;

        public CancellationToken ApplicationStopping =>
            _applicationStopping.Token;

        /// <summary>
        /// Signals that the application is stopping without disposing the
        /// cancellation source while active operations still observe its token.
        /// </summary>
        internal void Stop()
        {
            if (Interlocked.Exchange(ref _stopping, 1) != 0)
            {
                return;
            }

            _applicationStopping.Cancel();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            Stop();
            _applicationStopping.Dispose();
        }
    }
}