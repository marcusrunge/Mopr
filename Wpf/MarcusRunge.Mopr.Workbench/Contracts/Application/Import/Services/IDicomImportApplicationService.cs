using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Services
{
    /// <summary>
    /// Provides the public application use case for importing DICOM files from a directory.
    /// </summary>
    public interface IDicomImportApplicationService
    {
        /// <summary>
        /// Imports DICOM files from the supplied directory into the active default repository.
        /// </summary>
        /// <param name="request">The public import request.</param>
        /// <param name="cancellationToken">Cancels the import operation.</param>
        /// <returns>The structured public import result.</returns>
        Task<DicomImportApplicationResult> ImportDirectoryAsync(DicomImportApplicationRequest request, CancellationToken cancellationToken = default);
    }
}