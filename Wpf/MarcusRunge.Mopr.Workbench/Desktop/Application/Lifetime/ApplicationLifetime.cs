using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using System;
using System.Threading;

namespace MarcusRunge.Mopr.Workbench.Application.Lifetime
{
    internal sealed class ApplicationLifetime : IApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource _applicationStopping = new();

        public CancellationToken ApplicationStopping => _applicationStopping.Token;

        public void Dispose()
        {
            _applicationStopping.Cancel();
            _applicationStopping.Dispose();
        }
    }
}