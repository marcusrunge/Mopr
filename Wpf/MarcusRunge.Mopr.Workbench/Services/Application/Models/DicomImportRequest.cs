using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Models
{
    /// <summary>
    /// Contains the user-selected source and behavior for a DICOM import operation.
    /// </summary>
    public sealed record DicomImportRequest
    {
        /// <summary>
        /// Initializes a request for an automatically detected DICOM import source.
        /// </summary>
        /// <param name="sourcePath">The selected source path.</param>
        /// <param name="allowOverwrite">Indicates whether conflicting repository files may be overwritten.</param>
        public DicomImportRequest(string sourcePath, bool allowOverwrite = false) : this(sourcePath, DicomImportSourceType.AutoDetect, allowOverwrite)
        {
        }

        /// <summary>
        /// Initializes a request with an explicitly specified DICOM import source type.
        /// </summary>
        /// <param name="sourcePath">The selected source path.</param>
        /// <param name="sourceType">The source type selected or detected by the application.</param>
        /// <param name="allowOverwrite">Indicates whether conflicting repository files may be overwritten.</param>
        public DicomImportRequest(string sourcePath, DicomImportSourceType sourceType, bool allowOverwrite = false)
        {
            SourcePath = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
            SourceType = sourceType;
            AllowOverwrite = allowOverwrite;
        }

        /// <summary>
        /// Gets a value indicating whether conflicting repository files may be overwritten.
        /// </summary>
        public bool AllowOverwrite { get; }

        /// <summary>
        /// Gets the selected import source path.
        /// </summary>
        public string SourcePath { get; }

        /// <summary>
        /// Gets the selected or automatically detected import source type.
        /// </summary>
        public DicomImportSourceType SourceType { get; }

        /// <summary>
        /// Gets the selected source path for compatibility with directory-based consumers.
        /// </summary>
        [Obsolete("Use SourcePath instead. This compatibility property will be removed after all directory-only consumers have migrated.")]
        public string SourceDirectoryPath => SourcePath;
    }
}