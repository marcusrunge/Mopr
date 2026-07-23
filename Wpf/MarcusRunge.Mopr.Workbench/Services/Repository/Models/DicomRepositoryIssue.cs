using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Represents a structured technical repository integrity issue.
    /// </summary>
    public sealed class DicomRepositoryIssue
    {
        /// <summary>
        /// Gets or sets the actual physical file path, if available.
        /// </summary>
        public string ActualFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SOP instance UID read from the physical file,
        /// if available.
        /// </summary>
        public string ActualSopInstanceUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the repository service
        /// resolved the issue automatically.
        /// </summary>
        public bool AutomaticallyResolved { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the issue can be resolved
        /// automatically without an ambiguous or destructive decision.
        /// </summary>
        public bool CanResolveAutomatically { get; set; }

        /// <summary>
        /// Gets or sets the UTC date and time when the issue was detected.
        /// </summary>
        public DateTime DetectedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the expected physical file path, if available.
        /// </summary>
        public string ExpectedFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expected SOP instance UID, if available.
        /// </summary>
        public string ExpectedSopInstanceUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets the unique identifier of the issue.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the identifier of the persisted instance,
        /// if one is associated with the issue.
        /// </summary>
        public int? InstanceId { get; set; }

        /// <summary>
        /// Gets or sets the technical issue type.
        /// </summary>
        public DicomRepositoryIssueType IssueType { get; set; }

        /// <summary>
        /// Gets or sets the UTC date and time when an automatic repair
        /// completed successfully.
        /// </summary>
        public DateTime? ResolvedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets technical diagnostic details.
        /// </summary>
        public string TechnicalDetails { get; set; } = string.Empty;
    }
}