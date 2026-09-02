using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Provides the persistent technical audit identity used for machine-wide setup changes.
    /// </summary>
    internal interface ISetupAuditIdentityProvider
    {
        /// <summary>
        /// Gets or creates the persistent technical audit identity used for machine-wide setup changes.
        /// </summary>
        /// <param name="cancellationToken">Cancels the identity resolution.</param>
        /// <returns>The persistent identifier of the technical setup user.</returns>
        Task<int> GetOrCreateUserIdAsync(CancellationToken cancellationToken = default);
    }
}