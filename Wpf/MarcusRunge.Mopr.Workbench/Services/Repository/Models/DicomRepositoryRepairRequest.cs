namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Represents a repository repair request.
    /// </summary>
    public sealed class DicomRepositoryRepairRequest
    {      
        /// <summary>
        /// Gets or sets a value indicating whether missing or misplaced files
        /// should be repaired.
        /// </summary>
        public bool RepairMissingFiles { get; set; } = true;

        /// <summary>
        /// Gets or sets the optional ID of the repository location to verify.
        /// When no ID is specified, all enabled repository locations are
        /// verified independently.
        /// </summary>
        public int? RepositoryLocationId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether persisted repository files
        /// should be verified.
        /// </summary>
        public bool VerifyFiles { get; set; } = true;
    }
}