namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Represents the result of a repository repair operation.
    /// </summary>
    public sealed class DicomRepositoryRepairResult
    {
        /// <summary>
        /// Gets the errors.
        /// </summary>
        public IList<string> Errors { get; } = [];

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