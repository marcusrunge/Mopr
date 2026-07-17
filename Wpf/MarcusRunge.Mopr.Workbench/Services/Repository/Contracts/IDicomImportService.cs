using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Contracts
{
    /// <summary>
    /// Provides functionality for importing DICOM data from external sources.
    /// </summary>
    public interface IDicomImportService
    {    
        /// <summary>
        /// Imports DICOM data from the specified source.
        /// </summary>
        /// <param name="request">
        /// The import request
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        /// <returns>
        /// The import result.
        /// </returns>
        Task<DicomImportResult> ImportAsync(DicomImportRequest request, CancellationToken cancellationToken = default);
    }
}