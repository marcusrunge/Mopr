using MarcusRunge.Mopr.Workbench.Contracts.Dicom.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Contracts.Dicom
{
    /// <summary>
    /// Provides the application-oriented entry point for importing DICOM files from a directory.
    /// </summary>
    public interface IDicomImportService
    {
        /// <summary>
        /// Imports DICOM files from the supplied directory into the active default repository.
        /// </summary>
        /// <param name="request">The application-oriented import request.</param>
        /// <param name="cancellationToken">Cancels the import operation.</param>
        /// <returns>The structured application-oriented import result.</returns>
        Task<DicomImportResult> ImportDirectoryAsync(DicomImportRequest request, CancellationToken cancellationToken = default);
    }
}