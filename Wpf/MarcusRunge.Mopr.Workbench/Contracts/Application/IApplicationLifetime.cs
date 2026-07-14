using System.Threading;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application
{
    public interface IApplicationLifetime
    {
        CancellationToken ApplicationStopping { get; }
    }
}