using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Application.Startup
{
    /// <summary>
    /// Resolves the startup navigation target for the current MOPR user.
    /// </summary>
    internal interface IUserStartupRouteService
    {
        /// <summary>
        /// Signs in the current operating-system user and determines the corresponding navigation target.
        /// </summary>
        /// <param name="cancellationToken">Cancels the user startup routing.</param>
        /// <returns>The application-wide Prism navigation target.</returns>
        Task<string> GetNavigationTargetAsync(CancellationToken cancellationToken = default);
    }
}