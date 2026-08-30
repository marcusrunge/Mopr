using MarcusRunge.Mopr.Workbench.Contracts.Miras.Enums;
using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Miras.Models
{
    /// <summary>
    /// Represents a structured integrity issue detected by MIRAS.
    /// </summary>
    public sealed class MirasIssue
    {
        /// <summary>
        /// Gets or sets the actual physical file path, if one was found.
        /// </summary>
        public string ActualFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SOP instance UID found in the physical file.
        /// </summary>
        public string ActualSopInstanceUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MIRAS alert level.
        /// </summary>
        public MirasAlertLevel AlertLevel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the issue can be resolved automatically.
        /// </summary>
        public bool CanResolveAutomatically { get; set; }

        /// <summary>
        /// Gets or sets the expected physical repository file path.
        /// </summary>
        public string ExpectedFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expected SOP instance UID.
        /// </summary>
        public string ExpectedSopInstanceUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets the unique identifier of this MIRAS issue.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the identifier of the persisted instance, if available.
        /// </summary>
        public int? InstanceId { get; set; }

        /// <summary>
        /// Gets or sets the issue state.
        /// </summary>
        public MirasIssueState IssueState { get; set; }

        /// <summary>
        /// Gets or sets the issue type.
        /// </summary>
        public MirasIssueType IssueType { get; set; }

        /// <summary>
        /// Gets or sets the UTC date and time when the issue was detected.
        /// </summary>
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the recommended MIRAS action.
        /// </summary>
        public MirasRecommendedAction RecommendedAction { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the affected repository location, if available.
        /// </summary>
        public int? RepositoryLocationId { get; set; }

        /// <summary>
        /// Gets or sets the UTC date and time when the issue was resolved.
        /// </summary>
        public DateTime? ResolvedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who resolved the issue.
        /// </summary>
        public int? ResolvedByUserId { get; set; }

        /// <summary>
        /// Gets or sets technical diagnostic details that must not be exposed as an unfiltered user message.
        /// </summary>
        public string TechnicalDetails { get; set; } = string.Empty;
    }
}