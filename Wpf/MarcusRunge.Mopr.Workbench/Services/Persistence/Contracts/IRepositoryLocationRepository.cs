using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Defines the contract for managing configured DICOM repository locations.
    /// </summary>
    public interface IRepositoryLocationRepository : IRepository<RepositoryLocation>
    {
        /// <summary>
        /// Gets all enabled repository locations.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>All enabled repository locations.</returns>
        Task<IList<RepositoryLocation>> GetEnabledAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the configured default repository location.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The default location, or null if no default exists.</returns>
        Task<RepositoryLocation?> GetDefaultAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a repository location by its absolute root path.
        /// </summary>
        /// <param name="rootPath">The absolute local or UNC root path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The matching repository location, or null.</returns>
        Task<RepositoryLocation?> GetByRootPathAsync(string rootPath, CancellationToken cancellationToken = default);
    }
}