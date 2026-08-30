using MarcusRunge.Mopr.Workbench.Contracts.Miras.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Contracts.Miras
{
    /// <summary>
    /// Provides application-oriented medical image recovery
    /// and assurance operations.
    /// </summary>
    public interface IMirasService
    {
        /// <summary>
        /// Checks the configured medical image repository without initiating an automatic repair.
        /// </summary>
        /// <param name="cancellationToken">Cancels the integrity check.</param>
        /// <returns>The aggregated MIRAS operation result.</returns>
        Task<MirasOperationResult> CheckRepositoryAsync(CancellationToken cancellationToken = default);
    }
}