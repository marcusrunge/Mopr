using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Models
{
    /// <summary>
    /// Contains the user-selected input for a directory-based DICOM import.
    /// </summary>
    public sealed record DicomImportRequest(string SourceDirectoryPath, bool AllowOverwrite = false)
    {
        /// <summary>
        /// Gets the directory selected as the import source.
        /// </summary>
        public string SourceDirectoryPath { get; } = SourceDirectoryPath ?? throw new ArgumentNullException(nameof(SourceDirectoryPath));

        /// <summary>
        /// Gets a value indicating whether conflicting repository files may be overwritten.
        /// </summary>
        public bool AllowOverwrite { get; } = AllowOverwrite;
    }
}