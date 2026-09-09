using System.Threading;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services
{
    public interface ILifetimeService
    {
        CancellationToken ApplicationStopping { get; }
    }
}