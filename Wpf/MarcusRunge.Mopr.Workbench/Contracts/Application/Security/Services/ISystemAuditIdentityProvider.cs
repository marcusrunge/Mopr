using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services
{
    /// <summary>
    /// Provides the persistent technical audit identity used for machine-wide
    /// MOPR infrastructure operations.
    /// </summary>
    public interface ISystemAuditIdentityProvider
    {
        /// <summary>
        /// Gets or creates the persistent technical audit identity used for
        /// machine-wide MOPR infrastructure operations.
        /// </summary>
        /// <param name="cancellationToken">Cancels the identity resolution.</param>
        /// <returns>The positive persistent identifier of the technical MOPR system user.</returns>
        Task<int> GetOrCreateUserIdAsync(CancellationToken cancellationToken = default);
    }
}