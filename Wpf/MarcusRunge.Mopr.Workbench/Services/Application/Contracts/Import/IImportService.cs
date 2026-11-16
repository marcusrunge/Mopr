using MarcusRunge.Mopr.Workbench.Services.Application.Models.Import;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import
{
    /// <summary>
    /// Provides the public application use cases for importing files and selecting supported import sources.
    /// </summary>
    public interface IImportService
    {
        /// <summary>
        /// Gets the DICOM import drive provider.
        /// </summary>
        IDicomImportDriveProvider? DicomImportDriveProvider { get; }

        /// <summary>
        /// Gets the DICOM import service.
        /// </summary>
        IDicomImportService? DicomImportService { get; }

        /// <summary>
        /// Resolves and validates a selected DICOM import source without modifying,
        /// mounting or establishing a connection to that source.
        /// </summary>
        /// <param name="request">The source-selection request.</param>
        /// <param name="cancellationToken">Cancels the source resolution.</param>
        /// <returns>The classified and validated source information.</returns>
        Task<DicomImportSourceResolution> ResolveSourceAsync(DicomImportRequest request, CancellationToken cancellationToken = default);
    }
}