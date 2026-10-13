using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Contracts
{
    /// <summary>
    /// Validates potential repository locations through controlled file-system operations.
    /// </summary>
    public interface IRepositoryLocationValidationService
    {
        /// <summary>
        /// Validates whether the specified directory exists and can be read from and written to.
        /// </summary>
        /// <param name="directoryPath">The directory path to validate.</param>
        /// <param name="cancellationToken">The token used to cancel the validation.</param>
        /// <returns>The structured repository-location validation result.</returns>
        Task<RepositoryLocationValidationResult> ValidateAsync(string directoryPath, CancellationToken cancellationToken = default);
    }
}