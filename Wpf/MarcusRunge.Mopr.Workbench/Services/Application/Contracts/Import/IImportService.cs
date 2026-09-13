namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import
{
    /// <summary>
    /// Provides the public application use case for importing files.
    /// </summary>
    public interface IImportService
    {
        /// <summary>
        /// Gets the DICOM import service.
        /// </summary>
        IDicomImportService? DicomImportService { get; }
    }
}