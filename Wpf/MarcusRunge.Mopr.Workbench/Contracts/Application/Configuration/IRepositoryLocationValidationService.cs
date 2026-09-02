using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration
{
    /// <summary>
    /// Validates potential machine-wide DICOM repository locations.
    /// </summary>
    public interface IRepositoryLocationValidationService
    {
        /// <summary>
        /// Validates whether the supplied directory can be used as a managed DICOM repository location.
        /// </summary>
        /// <param name="directoryPath">The local or UNC directory path to validate.</param>
        /// <param name="cancellationToken">Cancels the validation operation.</param>
        /// <returns>The repository-location validation result.</returns>
        Task<RepositoryLocationValidationResult> ValidateAsync(string directoryPath, CancellationToken cancellationToken = default);
    }
}