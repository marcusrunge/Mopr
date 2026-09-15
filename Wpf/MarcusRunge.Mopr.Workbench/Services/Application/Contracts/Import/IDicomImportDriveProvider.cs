namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import
{
    /// <summary>
    /// Provides operating-system drive information required for import-source classification.
    /// </summary>
    internal interface IDicomImportDriveProvider
    {
        /// <summary>
        /// Gets the available drives visible to the current process.
        /// </summary>
        /// <returns>The available drive descriptors.</returns>
        IReadOnlyList<DicomImportDriveInfo> GetDrives();
    }
}