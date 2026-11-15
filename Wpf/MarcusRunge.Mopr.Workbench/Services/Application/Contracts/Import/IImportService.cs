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
    }
}