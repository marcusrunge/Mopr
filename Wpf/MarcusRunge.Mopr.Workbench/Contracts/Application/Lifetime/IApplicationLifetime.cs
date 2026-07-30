using System.Threading;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime
{
    public interface IApplicationLifetime
    {
        CancellationToken ApplicationStopping { get; }
    }
}