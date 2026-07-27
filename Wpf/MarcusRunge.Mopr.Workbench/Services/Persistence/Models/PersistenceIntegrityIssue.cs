using MarcusRunge.Mopr.Workbench.Services.Persistence.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Models
{
    /// <summary>
    /// Represents a structured technical Persistence integrity issue.
    /// </summary>
    public sealed class PersistenceIntegrityIssue
    {
        /// <summary>
        /// Gets or sets the UTC date and time when the issue was detected.
        /// </summary>
        public DateTime DetectedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the persisted entity type affected by the issue.
        /// </summary>
        public PersistenceIntegrityEntityType EntityType { get; set; }

        /// <summary>
        /// Gets or sets the ID of the affected persisted entity.
        /// </summary>
        public int? EntityId { get; set; }

        /// <summary>
        /// Gets the unique identifier of this issue.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the technical integrity issue type.
        /// </summary>
        public PersistenceIntegrityIssueType IssueType { get; set; }

        /// <summary>
        /// Gets or sets the name of the affected property or relationship.
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the referenced entity type, if the issue concerns
        /// a persisted relationship.
        /// </summary>
        public PersistenceIntegrityEntityType ReferencedEntityType { get; set; }

        /// <summary>
        /// Gets or sets the referenced entity ID, if available.
        /// </summary>
        public int? ReferencedEntityId { get; set; }

        /// <summary>
        /// Gets or sets technical diagnostic details.
        /// </summary>
        public string TechnicalDetails { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the affected persisted value, if available.
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}