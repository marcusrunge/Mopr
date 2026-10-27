using MarcusRunge.Mopr.Workbench.Services.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Services.Miras.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Test
{
    public sealed class MirasImagingReleasePolicyTests
    {
        [Fact]
        public void CanOpenImaging_WithRepositoryUnavailableActionRequired_ReturnsFalse()
        {
            var result = CreateResult(CreateIssue(MirasIssueType.RepositoryUnavailable, MirasIssueState.ActionRequired));

            Assert.False(MirasImagingReleasePolicy.CanOpenImaging(result));
        }

        [Fact]
        public void CanOpenImaging_WithDeferredRepositoryUnavailable_ReturnsTrue()
        {
            var result = CreateResult(CreateIssue(MirasIssueType.RepositoryUnavailable, MirasIssueState.Deferred));

            Assert.True(MirasImagingReleasePolicy.CanOpenImaging(result));
        }

        [Fact]
        public void CanOpenImaging_WithDeferredRepositoryUnavailableAndTechnicalError_ReturnsFalse()
        {
            var result = CreateResult(CreateIssue(MirasIssueType.RepositoryUnavailable, MirasIssueState.Deferred));
            result.TechnicalErrors.Add("Technical verification error.");

            Assert.False(MirasImagingReleasePolicy.CanOpenImaging(result));
        }

        [Theory]
        [InlineData(MirasIssueType.IdentityMismatch)]
        [InlineData(MirasIssueType.PersistenceRelationshipConflict)]
        public void CanOpenImaging_WithDeferredIntegrityIssue_ReturnsFalse(MirasIssueType issueType)
        {
            var result = CreateResult(CreateIssue(issueType, MirasIssueState.Deferred));

            Assert.False(MirasImagingReleasePolicy.CanOpenImaging(result));
        }

        [Fact]
        public void CanOpenImaging_WithDeferredRepositoryUnavailableAndActionRequiredIssue_ReturnsFalse()
        {
            var result = CreateResult(
                CreateIssue(MirasIssueType.RepositoryUnavailable, MirasIssueState.Deferred),
                CreateIssue(MirasIssueType.MissingFile, MirasIssueState.ActionRequired));

            Assert.False(MirasImagingReleasePolicy.CanOpenImaging(result));
        }

        [Fact]
        public void CanOpenImaging_WithDeferredRepositoryUnavailableWithoutUsableRepository_ReturnsFalse()
        {
            var result = CreateResult(CreateIssue(MirasIssueType.RepositoryUnavailable, MirasIssueState.Deferred));
            result.AssessedRepositoryLocations = 1;
            result.UsableRepositoryLocations = 0;

            Assert.False(MirasImagingReleasePolicy.CanOpenImaging(result));
        }

        [Fact]
        public void CanOpenImaging_WithCompletedResultAndNoIssues_ReturnsTrue()
        {
            var result = new MirasOperationResult { Status = MirasOperationStatus.Completed };

            Assert.True(MirasImagingReleasePolicy.CanOpenImaging(result));
        }

        [Fact]
        public void CanOpenImaging_WithBlockedResult_ReturnsFalse()
        {
            var result = new MirasOperationResult { Status = MirasOperationStatus.Blocked };

            Assert.False(MirasImagingReleasePolicy.CanOpenImaging(result));
        }

        private static MirasIssue CreateIssue(MirasIssueType issueType, MirasIssueState issueState) => new()
        {
            IsSessionDeferralAllowed = issueType == MirasIssueType.RepositoryUnavailable,
            IssueState = issueState,
            IssueType = issueType
        };

        private static MirasOperationResult CreateResult(params MirasIssue[] issues)
        {
            var result = new MirasOperationResult
            {
                AssessedRepositoryLocations = 2,
                Status = MirasOperationStatus.CompletedWithIssues,
                UsableRepositoryLocations = 1
            };

            foreach (var issue in issues) result.Issues.Add(issue);

            return result;
        }
    }
}
