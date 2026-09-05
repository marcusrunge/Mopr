using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Security
{
    /// <summary>
    /// Resolves the persistent audit identity of the current application user.
    /// </summary>
    public interface IAuditIdentityProvider
    {
        /// <summary>
        /// Gets the persistent identifier of the current application user.
        /// </summary>
        /// <param name="cancellationToken">Cancels the identity resolution.</param>
        /// <returns>
        /// The positive persistent user identifier, or <see langword="null"/> when
        /// no valid persistent audit identity is available.
        /// </returns>
        Task<int?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default);
    }
}