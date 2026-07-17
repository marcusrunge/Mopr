using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Contracts
{
    /// <summary>
    /// Provides functionality for repairing repository inconsistencies.
    /// </summary>
    public interface IDicomRepositoryRepairService
    {
        /// <summary>
        /// Repairs the repository.
        /// </summary>
        /// <param name="request">
        /// The repository repair request
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The repair result.
        /// </returns>
        Task<DicomRepositoryRepairResult> RepairAsync(DicomRepositoryRepairRequest request, CancellationToken cancellationToken = default);
    }
}