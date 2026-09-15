namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import
{
    /// <summary>
    /// Provides the base contract for import services.
    /// </summary>
    internal interface IImportServiceBase : IServiceBase
    {
        /// <summary>
        /// Gets the DICOM import drive provider.
        /// </summary>
        internal IDicomImportDriveProvider? DicomImportDriveProvider { get; }

        /// <summary>
        /// Gets the DICOM import source resolver.
        /// </summary>
        internal IDicomImportSourceResolver? DicomImportSourceResolver { get; }
    }
}