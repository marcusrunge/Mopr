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

            // Technical errors indicate that MIRAS could not complete a trustworthy
            // assessment. Session acknowledgement must never bypass this boundary.
            if (result.HasTechnicalErrors) return false;

            return result.Status switch
            {
                MirasOperationStatus.Completed => !result.HasIssues,
                MirasOperationStatus.CompletedWithIssues => result.UsableRepositoryLocations > 0 && result.Issues.All(CanOpenImagingWithIssue),
                _ => false
            };
        }

        private static bool CanOpenImagingWithIssue(MirasIssue issue)
        {
            ArgumentNullException.ThrowIfNull(issue);

            // A repository-availability finding may be acknowledged for the current
            // session. The affected repository remains unusable and will be assessed
            // again by the next MIRAS run.
            if (issue.IssueState == MirasIssueState.Deferred)
            {
                return issue.IssueType == MirasIssueType.RepositoryUnavailable && issue.IsSessionDeferralAllowed;
            }

            // Integrity findings remain fail-closed until MIRAS reports a state that
            // is already safe for continued application operation.
            return issue.IssueState is MirasIssueState.ActionAvailable or MirasIssueState.AutomaticallyResolved or MirasIssueState.ManuallyResolved;
        }
    }
}
