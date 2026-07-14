using MarcusRunge.Mopr.Workbench.Contracts.Application;
using System;
using System.Threading;

namespace MarcusRunge.Mopr.Workbench.Application
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