using MarcusRunge.Mopr.Workbench.Contracts.Miras.Enums;
using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Miras.Models
{
    /// <summary>
    /// Represents a localized and user-oriented MIRAS message.
    /// </summary>
    public sealed record MirasUserMessage
    {
        /// <summary>
        /// Gets the MIRAS alert level.
        /// </summary>
        public MirasAlertLevel AlertLevel { get; init; }

        /// <summary>
        /// Gets a value indicating whether the recommended action can be executed.
        /// </summary>
        public bool CanExecuteRecommendedAction { get; init; }

        /// <summary>
        /// Gets the user-oriented description.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Gets the identifier of the underlying MIRAS issue.
        /// </summary>
        public Guid IssueId { get; init; }

        /// <summary>
        /// Gets the current issue state.
        /// </summary>
        public MirasIssueState IssueState { get; init; }

        /// <summary>
        /// Gets the localized recommended action text.
        /// </summary>
        public string RecommendedActionText { get; init; } = string.Empty;

        /// <summary>
        /// Gets the localized status text.
        /// </summary>
        public string StatusText { get; init; } = string.Empty;

        /// <summary>
        /// Gets the technical diagnostic details.
        /// </summary>
        public string TechnicalDetails { get; init; } = string.Empty;

        /// <summary>
        /// Gets the localized title.
        /// </summary>
        public string Title { get; init; } = string.Empty;
    }
}