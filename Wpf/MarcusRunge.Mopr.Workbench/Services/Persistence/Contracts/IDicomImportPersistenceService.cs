using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Persists the Study, Series and Instance relationships for one imported
    /// DICOM file as one atomic Persistence operation.
    /// </summary>
    public interface IDicomImportPersistenceService
    {
        /// <summary>
        /// Validates and persists the relationships required for one imported
        /// DICOM instance.
        /// </summary>
        /// <param name="request">The atomic DICOM Persistence request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PersistAsync(DicomImportPersistenceRequest request, CancellationToken cancellationToken = default);
    }
}