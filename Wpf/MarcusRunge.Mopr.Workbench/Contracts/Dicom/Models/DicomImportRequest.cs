using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Dicom.Models
{
    /// <summary>
    /// Contains the user-selected input for a directory-based DICOM import.
    /// </summary>
    public sealed record DicomImportRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DicomImportRequest"/> class.
        /// </summary>
        /// <param name="sourceDirectoryPath">The directory selected as the import source.</param>
        /// <param name="allowOverwrite">Indicates whether conflicting repository files may be overwritten.</param>
        public DicomImportRequest(string sourceDirectoryPath, bool allowOverwrite = false)
        {
            SourceDirectoryPath = sourceDirectoryPath ?? throw new ArgumentNullException(nameof(sourceDirectoryPath));
            AllowOverwrite = allowOverwrite;
        }

        /// <summary>
        /// Gets a value indicating whether conflicting repository files may be overwritten.
        /// </summary>
        public bool AllowOverwrite { get; }

        /// <summary>
        /// Gets the directory selected as the import source.
        /// </summary>
        public string SourceDirectoryPath { get; }
    }
}