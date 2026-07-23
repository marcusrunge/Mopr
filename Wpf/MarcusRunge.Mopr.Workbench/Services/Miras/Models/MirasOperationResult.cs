using MarcusRunge.Mopr.Workbench.Services.Miras.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Models
{
    /// <summary>
    /// Represents the result of a MIRAS operation.
    /// </summary>
    public sealed class MirasOperationResult
    {
        /// <summary>
        /// Gets the number of issues that require an explicit action.
        /// </summary>
        public int ActionRequiredCount => Issues.Count(issue => issue.IssueState == MirasIssueState.ActionRequired);

        /// <summary>
        /// Gets the number of issues that were resolved automatically.
        /// </summary>
        public int AutomaticallyResolvedCount => Issues.Count(issue => issue.IssueState == MirasIssueState.AutomaticallyResolved);

        /// <summary>
        /// Gets or sets the UTC date and time when the operation completed.
        /// </summary>
        public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the duration of the operation.
        /// </summary>
        public TimeSpan Duration => CompletedAtUtc - StartedAtUtc;

        /// <summary>
        /// Gets a value indicating whether at least one issue requires an action.
        /// </summary>
        public bool HasActionRequired =>
            ActionRequiredCount > 0;

        /// <summary>
        /// Gets a value indicating whether any issues were detected.
        /// </summary>
        public bool HasIssues => Issues.Count > 0;

        /// <summary>
        /// Gets a value indicating whether technical errors occurred.
        /// </summary>
        public bool HasTechnicalErrors => TechnicalErrors.Count > 0;

        /// <summary>
        /// Gets the highest MIRAS alert level contained in the result.
        /// </summary>
        public MirasAlertLevel HighestAlertLevel => Issues.Count == 0 ? MirasAlertLevel.Normal : Issues.Max(issue => issue.AlertLevel);

        /// <summary>
        /// Gets the structured MIRAS issues detected during the operation.
        /// </summary>
        public IList<MirasIssue> Issues { get; } = [];

        /// <summary>
        /// Gets the localized user-oriented MIRAS messages.
        /// </summary>
        public IList<MirasUserMessage> Messages { get; } = [];

        /// <summary>
        /// Gets or sets the number of repository files or persisted instances
        /// inspected during the operation.
        /// </summary>
        public int ScannedItems { get; set; }

        /// <summary>
        /// Gets or sets the UTC date and time when the operation started.
        /// </summary>
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets technical errors that occurred during the operation.
        /// </summary>
        public IList<string> TechnicalErrors { get; } = [];
    }
}