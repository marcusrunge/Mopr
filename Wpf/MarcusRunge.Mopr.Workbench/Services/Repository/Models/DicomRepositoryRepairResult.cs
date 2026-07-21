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
        /// Gets or sets the number of files found at an unexpected repository location.
        /// </summary>
        public int MisplacedFiles { get; set; }

        /// <summary>
        /// Gets or sets the missing files.
        /// </summary>
        public int MissingFiles { get; set; }

        /// <summary>
        /// Gets or sets the repaired files.
        /// </summary>
        public int RepairedFiles { get; set; }

        /// <summary>
        /// Gets or sets the scanned files.
        /// </summary>
        public int ScannedFiles { get; set; }
    }
}