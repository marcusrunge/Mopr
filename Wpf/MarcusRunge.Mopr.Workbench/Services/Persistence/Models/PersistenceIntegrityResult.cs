namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Models
{
    /// <summary>
    /// Represents the result of a Persistence integrity verification.
    /// </summary>
    public sealed class PersistenceIntegrityResult
    {
        /// <summary>
        /// Gets the technical errors that prevented parts of the verification
        /// from completing.
        /// </summary>
        public IList<string> Errors { get; } = [];

        /// <summary>
        /// Gets the structured Persistence integrity issues.
        /// </summary>
        public IList<PersistenceIntegrityIssue> Issues { get; } = [];

        /// <summary>
        /// Gets or sets the number of persisted entities examined.
        /// </summary>
        public int ScannedEntities { get; set; }
    }
}