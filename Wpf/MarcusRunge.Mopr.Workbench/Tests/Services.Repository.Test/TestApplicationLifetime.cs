using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    internal sealed class TestApplicationLifetime : ILifetimeService, IDisposable
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