using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Defines the contract for a repository managing Series entities.
    /// </summary>
    public interface ISeriesRepository : IRepository<Series>
    {
        /// <summary>
        /// Gets a Series entity by its Instance UID.
        /// </summary>
        /// <param name="seriesInstanceUid">The Instance UID of the series.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the Series entity, or null if not found.</returns>
        Task<Series?> GetBySeriesInstanceUidAsync(string seriesInstanceUid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all Series entities associated with a specific Study.
        /// </summary>
        /// <param name="studyId">The ID of the study.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of Series entities.</returns>
        Task<IList<Series>> GetByStudyIdAsync(int studyId, CancellationToken cancellationToken = default);
    }
}