using MarcusRunge.Mopr.Workbench.Services.Miras.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Contracts
{
    /// <summary>
    /// Provides application-oriented medical image recovery
    /// and assurance operations.
    /// </summary>
    public interface IOperations
    {
        /// <summary>
        /// Checks the configured medical image repository without initiating an automatic repair.
        /// </summary>
        /// <param name="cancellationToken">Cancels the integrity check.</param>
        /// <returns>The aggregated MIRAS operation result.</returns>
        Task<MirasOperationResult> CheckRepositoryAsync(CancellationToken cancellationToken = default);
    }
}