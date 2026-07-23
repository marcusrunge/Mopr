using MarcusRunge.Mopr.Workbench.Services.Miras.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Contracts
{
    /// <summary>
    /// Provides application-oriented medical image recovery
    /// and assurance operations.
    /// </summary>
    public interface IMirasService
    {
        /// <summary>
        /// Checks the configured medical image repository.
        /// </summary>
        Task<MirasOperationResult> CheckRepositoryAsync(CancellationToken cancellationToken = default);
    }
}