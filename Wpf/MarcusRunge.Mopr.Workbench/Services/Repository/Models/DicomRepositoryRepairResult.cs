namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Represents the result of a repository repair operation.
    /// </summary>
    public sealed class DicomRepositoryRepairResult
    {
        /// <summary>
        /// Gets or sets the number of duplicate repository files.
        /// A duplicate is an additional physical file with an already discovered
        /// SOP instance UID.
        /// </summary>
        public int DuplicateFiles { get; set; }

        /// <summary>
        /// Gets the errors.
        /// </summary>
        public IList<string> Errors { get; } = [];

        /// <summary>
        /// Gets or sets the number of repository files whose DICOM identity
        /// does not match the persisted instance.
        /// </summary>
        public int IdentityMismatchFiles { get; set; }

        /// <summary>
        /// Gets or sets the number of temporary files left by incomplete import operations.
        /// </summary>
        public int IncompleteImportFiles { get; set; }

        /// <summary>
        /// Gets or sets the number of files at persisted repository locations
        /// that do not contain valid DICOM data.
        /// </summary>
        public int InvalidDicomFiles { get; set; }

        /// <summary>
        /// Gets the structured technical repository issues detected
        /// during verification or repair.
        /// </summary>
        public IList<DicomRepositoryIssue> Issues { get; } = [];

        /// <summary>
        /// Gets or sets the number of files found at an unexpected repository location.
        /// </summary>
        public int MisplacedFiles { get; set; }

        /// <summary>
        /// Gets or sets the missing files.
        /// </summary>
        public int MissingFiles { get; set; }

        /// <summary>
        /// Gets or sets the number of SOP instance identities for which valid
        /// physical DICOM data exists without a persisted instance.
        /// </summary>
        public int OrphanedFiles { get; set; }

        /// <summary>
        /// Gets or sets the number of inconsistent persistence relationships
        /// detected during repository verification.
        /// </summary>
        public int RelationshipConflicts { get; set; }

        /// <summary>
        /// Gets or sets the repaired files.
        /// </summary>
        public int RepairedFiles { get; set; }

        /// <summary>
        /// Gets or sets the number of configured repository locations that could not
        /// be accessed or inspected.
        /// </summary>
        public int UnavailableRepositoryLocations { get; set; }

        /// <summary>
        /// Gets or sets the number of persisted instances verified against the
        /// physical repository.
        /// </summary>
        public int ScannedFiles { get; set; }

        /// <summary>
        /// Gets or sets the number of repository files that could not be read or inspected.
        /// </summary>
        public int UnreadableFiles { get; set; }
    }
}