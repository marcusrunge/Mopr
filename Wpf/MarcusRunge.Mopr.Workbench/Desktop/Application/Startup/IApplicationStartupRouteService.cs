using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Application.Startup
{
    /// <summary>
    /// Determines the initial application navigation target.
    /// </summary>
    internal interface IApplicationStartupRouteService
    {
        /// <summary>
        /// Determines the initial navigation target from the machine-wide MOPR configuration.
        /// </summary>
        /// <param name="cancellationToken">Cancels the startup-route evaluation.</param>
        /// <returns>The application-wide Prism navigation target.</returns>
        Task<string> GetInitialNavigationTargetAsync(CancellationToken cancellationToken = default);
    }
}