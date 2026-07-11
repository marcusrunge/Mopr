using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Defines the contract for a repository that manages Instance entities.
    /// </summary>
    public interface IInstanceRepository : IRepository<Instance>
    {
        /// <summary>
        /// Gets all instances belonging to a specific series.
        /// </summary>
        /// <param name="seriesId">The ID of the series.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of instances for the specified series.</returns>
        Task<IList<Instance>> GetBySeriesIdAsync(int seriesId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an instance by its SOP instance UID.
        /// </summary>
        /// <param name="sopInstanceUid">The UID of the instance to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The instance if found; otherwise, null.</returns>
        Task<Instance?> GetBySopInstanceUidAsync(string sopInstanceUid, CancellationToken cancellationToken = default);
    }

}