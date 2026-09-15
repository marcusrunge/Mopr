using MarcusRunge.Mopr.Workbench.Services.Application.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import
{
    /// <summary>
    /// Describes the classified and validated source of a DICOM import operation.
    /// </summary>
    internal sealed record DicomImportSourceResolution
    {
        /// <summary>
        /// Gets a value indicating whether the source contains a DICOMDIR index in its root directory.
        /// </summary>
        internal bool ContainsDicomDirectoryIndex { get; init; }

        /// <summary>
        /// Gets a value indicating whether the selected source currently exists.
        /// </summary>
        internal bool Exists { get; init; }

        /// <summary>
        /// Gets the effective path that can be passed to a directory-based importer.
        /// </summary>
        internal string EffectiveSourcePath { get; init; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the source is currently ready for import.
        /// </summary>
        internal bool IsReady { get; init; }

        /// <summary>
        /// Gets a value indicating whether the source can use the existing directory import pipeline.
        /// </summary>
        internal bool IsRepositoryImportSupported { get; init; }

        /// <summary>
        /// Gets the source path originally supplied by the application.
        /// </summary>
        internal string OriginalSourcePath { get; init; } = string.Empty;

        /// <summary>
        /// Gets the resolved source type.
        /// </summary>
        internal DicomImportSourceType SourceType { get; init; } = DicomImportSourceType.Unknown;

        /// <summary>
        /// Gets a value indicating whether the source should be treated as read-only.
        /// </summary>
        internal bool TreatAsReadOnly { get; init; }
    }
}