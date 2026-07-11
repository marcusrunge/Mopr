using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Defines the contract for a repository managing User entities.
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        /// <summary>
        /// Gets all User entities.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of User entities.</returns>
        Task<IList<User>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a User entity by its login name.
        /// </summary>
        /// <param name="loginName">The login name of the user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the User entity, or null if not found.</returns>
        Task<User?> GetByLoginNameAsync(string loginName, CancellationToken cancellationToken = default);
    }
}