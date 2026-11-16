using MarcusRunge.Mopr.Workbench.Services.Application.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.Import
{
    /// <summary>
    /// Describes the classified and validated source of a DICOM import operation.
    /// </summary>
    public sealed record DicomImportSourceResolution
    {
        /// <summary>
        /// Gets a value indicating whether the source contains a DICOMDIR index in its root directory.
        /// </summary>
        public bool ContainsDicomDirectoryIndex { get; init; }

        /// <summary>
        /// Gets the effective path that may be passed to the repository import operation.
        /// </summary>
        public string EffectiveSourcePath { get; init; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the selected source exists.
        /// </summary>
        public bool Exists { get; init; }

        /// <summary>
        /// Gets a value indicating whether the source is currently ready for access.
        /// </summary>
        public bool IsReady { get; init; }

        /// <summary>
        /// Gets a value indicating whether the source can be processed by the repository importer.
        /// </summary>
        public bool IsRepositoryImportSupported { get; init; }

        /// <summary>
        /// Gets the source path originally supplied by the caller.
        /// </summary>
        public string OriginalSourcePath { get; init; } = string.Empty;

        /// <summary>
        /// Gets the classified source type.
        /// </summary>
        public DicomImportSourceType SourceType { get; init; }

        /// <summary>
        /// Gets a value indicating whether the source must be treated as read-only.
        /// </summary>
        public bool TreatAsReadOnly { get; init; }
    }
}