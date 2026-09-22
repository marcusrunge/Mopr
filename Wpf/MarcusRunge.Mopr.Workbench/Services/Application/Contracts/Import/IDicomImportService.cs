using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import
{
    /// <summary>
    /// Provides the public application use case for importing DICOM data from supported sources.
    /// </summary>
    public interface IDicomImportService
    {
        /// <summary>
        /// Imports DICOM data from the supplied source into the active default repository.
        /// </summary>
        /// <param name="request">The public import request.</param>
        /// <param name="cancellationToken">Cancels the import operation.</param>
        /// <returns>The structured public import result.</returns>
        Task<DicomImportResult> ImportAsync(DicomImportRequest request, CancellationToken cancellationToken = default);
    }
}