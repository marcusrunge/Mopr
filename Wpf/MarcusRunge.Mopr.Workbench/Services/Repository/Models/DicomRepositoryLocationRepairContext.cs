using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Holds the isolated file index and scan state for one repository
    /// location during a repair operation.
    /// </summary>
    internal sealed class DicomRepositoryLocationRepairContext
    {
        /// <summary>
        /// Gets the physical file paths already associated with persisted
        /// instances in this location.
        /// </summary>
        internal HashSet<string> AssociatedFilePaths { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the temporary files left by incomplete imports.
        /// </summary>
        internal HashSet<string> IncompleteImportFilePaths { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the SOP-instance-based file index for this location.
        /// </summary>
        internal Dictionary<string, DicomRepositoryFileIndexEntry> RepositoryFiles { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the repository location represented by this context.
        /// </summary>
        internal required RepositoryLocation RepositoryLocation { get; init; }

        /// <summary>
        /// Gets the normalized absolute repository root path.
        /// </summary>
        internal required string RepositoryRootPath { get; init; }

        /// <summary>
        /// Gets the unreadable physical files found while indexing this
        /// location.
        /// </summary>
        internal HashSet<string> UnreadableFilePaths { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}