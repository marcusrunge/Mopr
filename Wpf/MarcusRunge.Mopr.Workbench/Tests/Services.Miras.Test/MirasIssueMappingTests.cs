using MarcusRunge.Mopr.Workbench.Contracts.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Test
{
    public sealed class MirasIssueMappingTests
    {
        [Theory]
        [InlineData(PersistenceIntegrityIssueType.Unknown, MirasIssueType.Unknown, MirasAlertLevel.Warning, MirasRecommendedAction.ContactAdministrator)]
        [InlineData(PersistenceIntegrityIssueType.MissingRequiredValue, MirasIssueType.PersistenceRequiredValueMissing, MirasAlertLevel.Warning, MirasRecommendedAction.ContactAdministrator)]
        [InlineData(PersistenceIntegrityIssueType.InvalidValue, MirasIssueType.PersistenceValueInvalid, MirasAlertLevel.Warning, MirasRecommendedAction.ContactAdministrator)]
        [InlineData(PersistenceIntegrityIssueType.DuplicateUniqueValue, MirasIssueType.PersistenceUniqueValueConflict, MirasAlertLevel.Warning, MirasRecommendedAction.ReviewConflict)]
        [InlineData(PersistenceIntegrityIssueType.MissingParent, MirasIssueType.PersistenceRelationshipConflict, MirasAlertLevel.Warning, MirasRecommendedAction.ReviewConflict)]
        [InlineData(PersistenceIntegrityIssueType.InvalidAuditReference, MirasIssueType.PersistenceAuditReferenceInvalid, MirasAlertLevel.Caution, MirasRecommendedAction.ContactAdministrator)]
        public async Task CheckRepositoryAsync_MapsPersistenceIssue(PersistenceIntegrityIssueType sourceIssueType, MirasIssueType expectedIssueType, MirasAlertLevel expectedAlertLevel, MirasRecommendedAction expectedRecommendedAction)
        {
            using var context = new MirasServiceTestContext();
            var persistenceIssue = CreatePersistenceIssue(sourceIssueType);
            var persistenceResult = new PersistenceIntegrityResult
            {
                ScannedEntities = 1
            };

            persistenceResult.Issues.Add(persistenceIssue);
            context.ConfigurePersistenceResult(persistenceResult);

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Blocked, result.Status);
            Assert.Equal(expectedAlertLevel, result.HighestAlertLevel);
            Assert.Equal(1, result.ScannedItems);
            Assert.True(result.HasIssues);
            Assert.True(result.HasActionRequired);
            Assert.False(result.HasTechnicalErrors);

            var issue = Assert.Single(result.Issues);
            Assert.Equal(persistenceIssue.Id, issue.Id);
            Assert.Equal(expectedIssueType, issue.IssueType);
            Assert.Equal(expectedAlertLevel, issue.AlertLevel);
            Assert.Equal(MirasIssueState.ActionRequired, issue.IssueState);
            Assert.Equal(expectedRecommendedAction, issue.RecommendedAction);
            Assert.False(issue.CanResolveAutomatically);
            Assert.Equal(persistenceIssue.DetectedAtUtc, issue.OccurredAtUtc);
            Assert.Contains("TechnicalProperty", issue.TechnicalDetails, StringComparison.Ordinal);
            Assert.Contains("TechnicalValue", issue.TechnicalDetails, StringComparison.Ordinal);
            Assert.Contains("Technical persistence details", issue.TechnicalDetails, StringComparison.Ordinal);

            var issueMessage = Assert.Single(result.Messages, message => message.IssueId == issue.Id);
            Assert.Equal(expectedAlertLevel, issueMessage.AlertLevel);
            Assert.Equal(MirasIssueState.ActionRequired, issueMessage.IssueState);
            Assert.False(issueMessage.CanExecuteRecommendedAction);
            Assert.False(string.IsNullOrWhiteSpace(issueMessage.Title));
            Assert.False(string.IsNullOrWhiteSpace(issueMessage.Description));
            Assert.False(string.IsNullOrWhiteSpace(issueMessage.StatusText));
            Assert.False(string.IsNullOrWhiteSpace(issueMessage.RecommendedActionText));
            Assert.Empty(issueMessage.TechnicalDetails);
            Assert.DoesNotContain("TechnicalProperty", issueMessage.Description, StringComparison.Ordinal);
            Assert.DoesNotContain("TechnicalValue", issueMessage.Description, StringComparison.Ordinal);
            Assert.DoesNotContain("Technical persistence details", issueMessage.Description, StringComparison.Ordinal);

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryNotCalled();
        }

        [Fact]
        public async Task CheckRepositoryAsync_MapsPersistenceInstanceId()
        {
            using var context = new MirasServiceTestContext();
            var persistenceResult = new PersistenceIntegrityResult();

            persistenceResult.Issues.Add(CreatePersistenceIssue(PersistenceIntegrityIssueType.MissingParent, PersistenceIntegrityEntityType.Instance, entityId: 41));

            context.ConfigurePersistenceResult(persistenceResult);

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            var issue = Assert.Single(result.Issues);
            Assert.Equal(41, issue.InstanceId);
            Assert.Null(issue.RepositoryLocationId);

            context.VerifyRepositoryNotCalled();
        }

        [Fact]
        public async Task CheckRepositoryAsync_MapsPersistenceRepositoryLocationId()
        {
            using var context = new MirasServiceTestContext();
            var persistenceResult = new PersistenceIntegrityResult();

            persistenceResult.Issues.Add(CreatePersistenceIssue(PersistenceIntegrityIssueType.InvalidValue, PersistenceIntegrityEntityType.RepositoryLocation, entityId: 17));

            context.ConfigurePersistenceResult(persistenceResult);

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            var issue = Assert.Single(result.Issues);
            Assert.Null(issue.InstanceId);
            Assert.Equal(17, issue.RepositoryLocationId);

            context.VerifyRepositoryNotCalled();
        }

        [Theory]
        [InlineData(DicomRepositoryIssueType.Unknown, MirasIssueType.Unknown, MirasAlertLevel.Warning, MirasRecommendedAction.ContactAdministrator)]
        [InlineData(DicomRepositoryIssueType.MissingFile, MirasIssueType.MissingFile, MirasAlertLevel.Caution, MirasRecommendedAction.LocateFile)]
        [InlineData(DicomRepositoryIssueType.MisplacedFile, MirasIssueType.MisplacedFile, MirasAlertLevel.Caution, MirasRecommendedAction.RestoreExpectedLocation)]
        [InlineData(DicomRepositoryIssueType.DuplicateFile, MirasIssueType.DuplicateFile, MirasAlertLevel.Caution, MirasRecommendedAction.ReviewDuplicate)]
        [InlineData(DicomRepositoryIssueType.IdentityMismatch, MirasIssueType.IdentityMismatch, MirasAlertLevel.Warning, MirasRecommendedAction.ReviewConflict)]
        [InlineData(DicomRepositoryIssueType.OrphanedFile, MirasIssueType.OrphanedFile, MirasAlertLevel.Caution, MirasRecommendedAction.RebuildPersistenceEntry)]
        [InlineData(DicomRepositoryIssueType.InvalidDicomFile, MirasIssueType.InvalidDicomFile, MirasAlertLevel.Warning, MirasRecommendedAction.ReviewInvalidFile)]
        [InlineData(DicomRepositoryIssueType.UnreadableFile, MirasIssueType.UnreadableFile, MirasAlertLevel.Warning, MirasRecommendedAction.ContactAdministrator)]
        [InlineData(DicomRepositoryIssueType.IncompleteImport, MirasIssueType.IncompleteImport, MirasAlertLevel.Warning, MirasRecommendedAction.ReviewConflict)]
        [InlineData(DicomRepositoryIssueType.RepositoryLocationUnavailable, MirasIssueType.RepositoryUnavailable, MirasAlertLevel.Warning, MirasRecommendedAction.ReconnectRepository)]
        [InlineData(DicomRepositoryIssueType.RelationshipConflict, MirasIssueType.RelationshipConflict, MirasAlertLevel.Warning, MirasRecommendedAction.ReviewConflict)]
        public async Task CheckRepositoryAsync_MapsRepositoryIssue(DicomRepositoryIssueType sourceIssueType, MirasIssueType expectedIssueType, MirasAlertLevel expectedAlertLevel, MirasRecommendedAction expectedRecommendedAction)
        {
            using var context = new MirasServiceTestContext();
            var repositoryIssue = CreateRepositoryIssue(sourceIssueType);
            var repositoryResult = new DicomRepositoryRepairResult
            {
                ScannedFiles = 1
            };

            context.ConfigurePersistenceResult(new PersistenceIntegrityResult());
            repositoryResult.Issues.Add(repositoryIssue);
            context.ConfigureRepositoryResult(repositoryResult);

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.CompletedWithIssues, result.Status);
            Assert.Equal(expectedAlertLevel, result.HighestAlertLevel);
            Assert.Equal(1, result.ScannedItems);
            Assert.True(result.HasIssues);
            Assert.True(result.HasActionRequired);
            Assert.False(result.HasTechnicalErrors);

            var issue = Assert.Single(result.Issues);
            Assert.Equal(repositoryIssue.Id, issue.Id);
            Assert.Equal(expectedIssueType, issue.IssueType);
            Assert.Equal(expectedAlertLevel, issue.AlertLevel);
            Assert.Equal(MirasIssueState.ActionRequired, issue.IssueState);
            Assert.Equal(expectedRecommendedAction, issue.RecommendedAction);
            Assert.False(issue.CanResolveAutomatically);
            Assert.Equal(repositoryIssue.InstanceId, issue.InstanceId);
            Assert.Equal(repositoryIssue.RepositoryLocationId, issue.RepositoryLocationId);
            Assert.Equal(repositoryIssue.ExpectedFilePath, issue.ExpectedFilePath);
            Assert.Equal(repositoryIssue.ActualFilePath, issue.ActualFilePath);
            Assert.Equal(repositoryIssue.ExpectedSopInstanceUid, issue.ExpectedSopInstanceUid);
            Assert.Equal(repositoryIssue.ActualSopInstanceUid, issue.ActualSopInstanceUid);
            Assert.Equal(repositoryIssue.DetectedAtUtc, issue.OccurredAtUtc);
            Assert.Equal(repositoryIssue.TechnicalDetails, issue.TechnicalDetails);

            var message = Assert.Single(result.Messages);
            Assert.Equal(issue.Id, message.IssueId);
            Assert.Equal(expectedAlertLevel, message.AlertLevel);
            Assert.Equal(MirasIssueState.ActionRequired, message.IssueState);
            Assert.False(message.CanExecuteRecommendedAction);
            Assert.False(string.IsNullOrWhiteSpace(message.Title));
            Assert.False(string.IsNullOrWhiteSpace(message.Description));
            Assert.False(string.IsNullOrWhiteSpace(message.StatusText));
            Assert.False(string.IsNullOrWhiteSpace(message.RecommendedActionText));
            Assert.Empty(message.TechnicalDetails);
            Assert.DoesNotContain(repositoryIssue.ExpectedFilePath, message.Description, StringComparison.Ordinal);
            Assert.DoesNotContain(repositoryIssue.ActualFilePath, message.Description, StringComparison.Ordinal);
            Assert.DoesNotContain(repositoryIssue.ExpectedSopInstanceUid, message.Description, StringComparison.Ordinal);
            Assert.DoesNotContain(repositoryIssue.ActualSopInstanceUid, message.Description, StringComparison.Ordinal);
            Assert.DoesNotContain(repositoryIssue.TechnicalDetails, message.Description, StringComparison.Ordinal);

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryCalledOnce();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WithAutomaticallyResolvableIssue_ReturnsActionAvailableWithoutRepairing()
        {
            using var context = new MirasServiceTestContext();
            var repositoryIssue = CreateRepositoryIssue(DicomRepositoryIssueType.MisplacedFile);
            var repositoryResult = new DicomRepositoryRepairResult();

            repositoryIssue.CanResolveAutomatically = true;
            repositoryResult.Issues.Add(repositoryIssue);

            context.ConfigurePersistenceResult(new PersistenceIntegrityResult());
            context.ConfigureRepositoryResult(repositoryResult);

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.CompletedWithIssues, result.Status);
            Assert.False(result.HasActionRequired);
            Assert.Equal(0, result.ActionRequiredCount);
            Assert.Equal(0, result.AutomaticallyResolvedCount);

            var issue = Assert.Single(result.Issues);
            Assert.Equal(MirasIssueState.ActionAvailable, issue.IssueState);
            Assert.True(issue.CanResolveAutomatically);
            Assert.Null(issue.ResolvedAtUtc);

            var message = Assert.Single(result.Messages);
            Assert.Equal(MirasIssueState.ActionAvailable, message.IssueState);
            Assert.True(message.CanExecuteRecommendedAction);
            Assert.Empty(message.TechnicalDetails);

            context.VerifyRepositoryCalledOnce();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WithAutomaticallyResolvedIssue_ReturnsResolvedState()
        {
            using var context = new MirasServiceTestContext();
            var resolvedAtUtc = DateTime.UtcNow;
            var repositoryIssue = CreateRepositoryIssue(DicomRepositoryIssueType.MisplacedFile);
            var repositoryResult = new DicomRepositoryRepairResult();

            repositoryIssue.AutomaticallyResolved = true;
            repositoryIssue.CanResolveAutomatically = true;
            repositoryIssue.ResolvedAtUtc = resolvedAtUtc;
            repositoryResult.Issues.Add(repositoryIssue);

            context.ConfigurePersistenceResult(new PersistenceIntegrityResult());
            context.ConfigureRepositoryResult(repositoryResult);

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.CompletedWithIssues, result.Status);
            Assert.False(result.HasActionRequired);
            Assert.Equal(0, result.ActionRequiredCount);
            Assert.Equal(1, result.AutomaticallyResolvedCount);

            var issue = Assert.Single(result.Issues);
            Assert.Equal(MirasIssueState.AutomaticallyResolved, issue.IssueState);
            Assert.Equal(resolvedAtUtc, issue.ResolvedAtUtc);

            var message = Assert.Single(result.Messages);
            Assert.Equal(MirasIssueState.AutomaticallyResolved, message.IssueState);
            Assert.False(message.CanExecuteRecommendedAction);
            Assert.Empty(message.TechnicalDetails);

            context.VerifyRepositoryCalledOnce();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WithMultipleRepositoryIssues_AggregatesAllIssuesAndUsesHighestAlertLevel()
        {
            using var context = new MirasServiceTestContext();
            var repositoryResult = new DicomRepositoryRepairResult
            {
                ScannedFiles = 7
            };

            repositoryResult.Issues.Add(CreateRepositoryIssue(DicomRepositoryIssueType.MissingFile));
            repositoryResult.Issues.Add(CreateRepositoryIssue(DicomRepositoryIssueType.IdentityMismatch));

            context.ConfigurePersistenceResult(new PersistenceIntegrityResult
            {
                ScannedEntities = 13
            });
            context.ConfigureRepositoryResult(repositoryResult);

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.CompletedWithIssues, result.Status);
            Assert.Equal(MirasAlertLevel.Warning, result.HighestAlertLevel);
            Assert.Equal(20, result.ScannedItems);
            Assert.Equal(2, result.Issues.Count);
            Assert.Equal(2, result.Messages.Count);
            Assert.Equal(2, result.ActionRequiredCount);
            Assert.True(result.HasActionRequired);
            Assert.Contains(result.Issues, issue => issue.IssueType == MirasIssueType.MissingFile);
            Assert.Contains(result.Issues, issue => issue.IssueType == MirasIssueType.IdentityMismatch);

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryCalledOnce();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WithMultiplePersistenceIssues_CollectsAllAndBlocksRepository()
        {
            using var context = new MirasServiceTestContext();
            var persistenceResult = new PersistenceIntegrityResult
            {
                ScannedEntities = 9
            };

            persistenceResult.Issues.Add(CreatePersistenceIssue(
                PersistenceIntegrityIssueType.InvalidAuditReference,
                PersistenceIntegrityEntityType.User,
                entityId: 3));

            persistenceResult.Issues.Add(CreatePersistenceIssue(
                PersistenceIntegrityIssueType.DuplicateUniqueValue,
                PersistenceIntegrityEntityType.Instance,
                entityId: 8));

            context.ConfigurePersistenceResult(persistenceResult);

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Blocked, result.Status);
            Assert.Equal(MirasAlertLevel.Warning, result.HighestAlertLevel);
            Assert.Equal(9, result.ScannedItems);
            Assert.Equal(2, result.Issues.Count);
            Assert.Equal(3, result.Messages.Count);
            Assert.Equal(2, result.ActionRequiredCount);
            Assert.True(result.HasActionRequired);
            Assert.Contains(result.Issues, issue => issue.IssueType == MirasIssueType.PersistenceAuditReferenceInvalid);
            Assert.Contains(result.Issues, issue => issue.IssueType == MirasIssueType.PersistenceUniqueValueConflict);

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryNotCalled();
        }

        private static PersistenceIntegrityIssue CreatePersistenceIssue(
            PersistenceIntegrityIssueType issueType,
            PersistenceIntegrityEntityType entityType = PersistenceIntegrityEntityType.Instance,
            int? entityId = 11) =>
            new()
            {
                DetectedAtUtc = DateTime.UtcNow,
                EntityId = entityId,
                EntityType = entityType,
                IssueType = issueType,
                PropertyName = "TechnicalProperty",
                ReferencedEntityId = 19,
                ReferencedEntityType = PersistenceIntegrityEntityType.Series,
                TechnicalDetails = "Technical persistence details",
                Value = "TechnicalValue"
            };

        private static DicomRepositoryIssue CreateRepositoryIssue(DicomRepositoryIssueType issueType) =>
            new()
            {
                ActualFilePath = @"C:\Sensitive\actual-image.dcm",
                ActualSeriesInstanceUid = "1.2.840.actual.series",
                ActualSopInstanceUid = "1.2.840.actual.instance",
                ActualStudyInstanceUid = "1.2.840.actual.study",
                AutomaticallyResolved = false,
                CanResolveAutomatically = false,
                DetectedAtUtc = DateTime.UtcNow,
                ExpectedFilePath = @"C:\Sensitive\expected-image.dcm",
                ExpectedSeriesInstanceUid = "1.2.840.expected.series",
                ExpectedSopInstanceUid = "1.2.840.expected.instance",
                ExpectedStudyInstanceUid = "1.2.840.expected.study",
                InstanceId = 31,
                IssueType = issueType,
                RecoveryCandidateFilePath = @"C:\Sensitive\recovery-image.dcm",
                RepositoryLocationId = 7,
                TechnicalDetails = "Technical repository details"
            };
    }
}