namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Models
{
    /// <summary>
    /// Represents a Persistence integrity verification request.
    /// </summary>
    public sealed class PersistenceIntegrityRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether audit user references
        /// should be verified.
        /// </summary>
        public bool VerifyAuditReferences { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether required persisted values
        /// should be verified.
        /// </summary>
        public bool VerifyRequiredValues { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether persisted parent-child
        /// relationships should be verified.
        /// </summary>
        public bool VerifyRelationships { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether values with a uniqueness
        /// requirement should be verified.
        /// </summary>
        public bool VerifyUniqueValues { get; set; } = true;
    }
}