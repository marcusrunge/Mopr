using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Tracks the compensable physical state established for one imported
    /// DICOM file.
    /// </summary>
    internal sealed class DicomImportFileSystemContext
    {
        /// <summary>
        /// Gets or sets the unique backup path containing the replaced original
        /// repository file.
        /// </summary>
        internal string BackupPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the validated absolute destination path.
        /// </summary>
        internal string DestinationPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the physical state established by the import.
        /// </summary>
        internal DicomImportFileSystemState State { get; set; }
    }
}