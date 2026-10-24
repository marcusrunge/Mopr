using MarcusRunge.Mopr.Workbench.Services.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Services.Miras.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Miras
{
    /// <summary>
    /// Defines whether a completed MIRAS assessment permits access to the imaging workbench.
    /// </summary>
    public static class MirasImagingReleasePolicy
    {
        /// <summary>
        /// Determines whether the supplied MIRAS result permits access to the imaging workbench.
        /// </summary>
        /// <param name="result">The completed MIRAS operation result.</param>
        /// <returns><see langword="true"/> when Imaging may be opened; otherwise, <see langword="false"/>.</returns>
        public static bool CanOpenImaging(MirasOperationResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            /*
             * Imaging is released only after a technically complete assessment
             * without an unresolved condition requiring explicit user action.
             * Unknown or internally inconsistent result states remain fail-closed.
             */
            if (result.HasTechnicalErrors) return false;

            return result.Status switch
            {
                MirasOperationStatus.Completed => !result.HasIssues,
                MirasOperationStatus.CompletedWithIssues => result.Issues.All(CanOpenImagingWithIssue),
                _ => false
            };
        }

        private static bool CanOpenImagingWithIssue(MirasIssue issue)
        {
            ArgumentNullException.ThrowIfNull(issue);

            // Only an unavailable repository explicitly acknowledged for this session
            // is non-blocking. Deferring any integrity conflict remains fail-closed.
            if (issue.IssueState == MirasIssueState.Deferred) return issue.IssueType == MirasIssueType.RepositoryUnavailable;

            return issue.IssueState is MirasIssueState.ActionAvailable or MirasIssueState.AutomaticallyResolved or MirasIssueState.ManuallyResolved;
        }
    }
}