using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Provides persistent access to MOPR users.
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        /// <summary>
        /// Gets the user assigned to the supplied operating-system login name.
        /// </summary>
        /// <param name="loginName">The operating-system login name.</param>
        /// <param name="cancellationToken">Cancels the lookup.</param>
        /// <returns>The assigned user, or <see langword="null"/> when no assignment exists.</returns>
        Task<User?> GetByLoginNameAsync(string loginName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the user assigned to the supplied Windows security identifier.
        /// </summary>
        /// <param name="securityIdentifier">The durable Windows security identifier.</param>
        /// <param name="cancellationToken">Cancels the lookup.</param>
        /// <returns>The assigned user, or <see langword="null"/> when no assignment exists.</returns>
        Task<User?> GetBySecurityIdentifierAsync(string securityIdentifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether at least one personal MOPR user exists.
        /// </summary>
        /// <param name="cancellationToken">Cancels the lookup.</param>
        /// <returns>
        /// <see langword="true"/> when a user other than the reserved technical
        /// MOPR system identity exists; otherwise, <see langword="false"/>.
        /// </returns>
        Task<bool> HasPersonalUsersAsync(CancellationToken cancellationToken = default);
    }
}