using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Defines the contract for a repository that manages Study entities.
    /// </summary>
    public interface IStudyRepository : IRepository<Study>
    {
        /// <summary>
        /// Gets all studies.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of all studies.</returns>
        Task<IList<Study>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a study by its instance UID.
        /// </summary>
        /// <param name="studyInstanceUid">The UID of the study to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The study if found; otherwise, null.</returns>
        Task<Study?> GetByStudyInstanceUidAsync(string studyInstanceUid, CancellationToken cancellationToken = default);
    }
}