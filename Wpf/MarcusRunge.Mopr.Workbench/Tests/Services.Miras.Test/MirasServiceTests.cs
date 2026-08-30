using MarcusRunge.Mopr.Workbench.Contracts.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Test
{
    public sealed class MirasServiceTests
    {
        [Fact]
        public async Task CheckRepositoryAsync_WithCleanPersistenceAndRepository_ReturnsCompletedResult()
        {
            using var context = new MirasServiceTestContext();
            context.ConfigurePersistenceResult(new PersistenceIntegrityResult
            {
                ScannedEntities = 12
            });
            context.ConfigureRepositoryResult(new DicomRepositoryRepairResult
            {
                ScannedFiles = 8
            });

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Completed, result.Status);
            Assert.Equal(MirasAlertLevel.Normal, result.HighestAlertLevel);
            Assert.Equal(20, result.ScannedItems);
            Assert.False(result.HasIssues);
            Assert.False(result.HasTechnicalErrors);
            Assert.False(result.HasActionRequired);
            Assert.Empty(result.Issues);
            Assert.Empty(result.TechnicalErrors);

            var message = Assert.Single(result.Messages);
            Assert.Equal(MirasAlertLevel.Normal, message.AlertLevel);
            Assert.False(message.CanExecuteRecommendedAction);
            Assert.False(string.IsNullOrWhiteSpace(message.Title));
            Assert.False(string.IsNullOrWhiteSpace(message.Description));
            Assert.False(string.IsNullOrWhiteSpace(message.StatusText));
            Assert.False(string.IsNullOrWhiteSpace(message.RecommendedActionText));
            Assert.Empty(message.TechnicalDetails);

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryCalledOnce();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WithBlockingPersistenceIssue_DoesNotInspectRepository()
        {
            using var context = new MirasServiceTestContext();
            var persistenceResult = new PersistenceIntegrityResult
            {
                ScannedEntities = 5
            };

            persistenceResult.Issues.Add(CreatePersistenceIssue(
                PersistenceIntegrityIssueType.MissingParent,
                PersistenceIntegrityEntityType.Instance,
                entityId: 42));

            context.ConfigurePersistenceResult(persistenceResult);

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Blocked, result.Status);
            Assert.Equal(MirasAlertLevel.Warning, result.HighestAlertLevel);
            Assert.Equal(5, result.ScannedItems);
            Assert.True(result.HasIssues);
            Assert.True(result.HasActionRequired);
            Assert.False(result.HasTechnicalErrors);

            var issue = Assert.Single(result.Issues);
            Assert.Equal(MirasIssueType.PersistenceRelationshipConflict, issue.IssueType);
            Assert.Equal(MirasIssueState.ActionRequired, issue.IssueState);
            Assert.Equal(MirasRecommendedAction.ReviewConflict, issue.RecommendedAction);
            Assert.Equal(42, issue.InstanceId);
            Assert.Null(issue.RepositoryLocationId);
            Assert.False(issue.CanResolveAutomatically);

            Assert.All(result.Messages, message =>
            {
                Assert.DoesNotContain("42", message.Title, StringComparison.Ordinal);
                Assert.DoesNotContain("42", message.Description, StringComparison.Ordinal);
                Assert.DoesNotContain("42", message.StatusText, StringComparison.Ordinal);
                Assert.Empty(message.TechnicalDetails);
            });

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryNotCalled();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WithPersistenceTechnicalErrors_ReturnsIncompleteWithoutInspectingRepository()
        {
            using var context = new MirasServiceTestContext();
            var persistenceResult = new PersistenceIntegrityResult
            {
                ScannedEntities = 3
            };

            persistenceResult.Errors.Add("Technical persistence error containing C:\\Sensitive\\database.data");
            context.ConfigurePersistenceResult(persistenceResult);

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Incomplete, result.Status);
            Assert.Equal(MirasAlertLevel.Warning, result.HighestAlertLevel);
            Assert.Equal(3, result.ScannedItems);
            Assert.False(result.HasIssues);
            Assert.True(result.HasTechnicalErrors);
            Assert.False(result.HasActionRequired);
            Assert.Single(result.TechnicalErrors);

            var message = Assert.Single(result.Messages);
            Assert.DoesNotContain("C:\\Sensitive", message.Title, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("C:\\Sensitive", message.Description, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("database.data", message.Description, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(message.TechnicalDetails);

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryNotCalled();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WithRepositoryTechnicalErrors_ReturnsIncompleteResult()
        {
            using var context = new MirasServiceTestContext();
            context.ConfigurePersistenceResult(new PersistenceIntegrityResult
            {
                ScannedEntities = 10
            });

            var repositoryResult = new DicomRepositoryRepairResult
            {
                ScannedFiles = 6
            };

            repositoryResult.Errors.Add("Technical repository error containing C:\\Sensitive\\image.dcm");
            context.ConfigureRepositoryResult(repositoryResult);

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Incomplete, result.Status);
            Assert.Equal(MirasAlertLevel.Warning, result.HighestAlertLevel);
            Assert.Equal(16, result.ScannedItems);
            Assert.False(result.HasIssues);
            Assert.True(result.HasTechnicalErrors);
            Assert.Single(result.TechnicalErrors);

            var message = Assert.Single(result.Messages);
            Assert.DoesNotContain("C:\\Sensitive", message.Title, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("C:\\Sensitive", message.Description, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("image.dcm", message.Description, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(message.TechnicalDetails);

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryCalledOnce();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WhenPersistenceServiceThrows_ReturnsFailedResult()
        {
            using var context = new MirasServiceTestContext();

            context.PersistenceIntegrityService.Setup(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Sensitive persistence failure"));

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Failed, result.Status);
            Assert.Equal(MirasAlertLevel.Warning, result.HighestAlertLevel);
            Assert.False(result.HasIssues);
            Assert.True(result.HasTechnicalErrors);
            Assert.Single(result.TechnicalErrors);

            var message = Assert.Single(result.Messages);
            Assert.DoesNotContain("Sensitive persistence failure", message.Title, StringComparison.Ordinal);
            Assert.DoesNotContain("Sensitive persistence failure", message.Description, StringComparison.Ordinal);
            Assert.Empty(message.TechnicalDetails);

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryNotCalled();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WhenRepositoryServiceThrows_ReturnsFailedResult()
        {
            using var context = new MirasServiceTestContext();
            context.ConfigurePersistenceResult(new PersistenceIntegrityResult());

            context.RepositoryRepairService.Setup(service => service.RepairAsync(It.IsAny<DicomRepositoryRepairRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Sensitive repository failure"));

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Failed, result.Status);
            Assert.Equal(MirasAlertLevel.Warning, result.HighestAlertLevel);
            Assert.False(result.HasIssues);
            Assert.True(result.HasTechnicalErrors);
            Assert.Single(result.TechnicalErrors);

            var message = Assert.Single(result.Messages);
            Assert.DoesNotContain("Sensitive repository failure", message.Title, StringComparison.Ordinal);
            Assert.DoesNotContain("Sensitive repository failure", message.Description, StringComparison.Ordinal);
            Assert.Empty(message.TechnicalDetails);

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryCalledOnce();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WithCanceledCallerToken_DoesNotStartPersistenceCheck()
        {
            using var context = new MirasServiceTestContext();
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            cancellationSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Service.CheckRepositoryAsync(cancellationSource.Token));

            context.PersistenceIntegrityService.Verify(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>()), Times.Never);

            context.VerifyRepositoryNotCalled();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WhenPersistenceCheckIsCanceled_PropagatesCancellation()
        {
            using var context = new MirasServiceTestContext();

            context.PersistenceIntegrityService.Setup(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>())).Returns<PersistenceIntegrityRequest, CancellationToken>((_, cancellationToken) => Task.FromCanceled<PersistenceIntegrityResult>(CreateCanceledToken(cancellationToken)));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken));

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryNotCalled();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WhenCancellationIsRequestedAfterPersistence_DoesNotInspectRepository()
        {
            using var context = new MirasServiceTestContext();
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

            context.PersistenceIntegrityService
                .Setup(service => service.VerifyAsync(
                    It.IsAny<PersistenceIntegrityRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    cancellationSource.Cancel();
                    return new PersistenceIntegrityResult();
                });

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => context.Service.CheckRepositoryAsync(cancellationSource.Token));

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryNotCalled();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WhenApplicationStops_PropagatesCancellation()
        {
            using var context = new MirasServiceTestContext();
            context.ApplicationLifetime.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken));

            context.PersistenceIntegrityService.Verify(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>()), Times.Never);

            context.VerifyRepositoryNotCalled();
        }

        [Fact]
        public async Task CheckRepositoryAsync_UsesSafeInspectionRequest()
        {
            using var context = new MirasServiceTestContext();
            DicomRepositoryRepairRequest? capturedRequest = null;

            context.ConfigurePersistenceResult(new PersistenceIntegrityResult());

            context.RepositoryRepairService.Setup(service => service.RepairAsync(It.IsAny<DicomRepositoryRepairRequest>(), It.IsAny<CancellationToken>())).Callback<DicomRepositoryRepairRequest, CancellationToken>((request, _) => capturedRequest = request).ReturnsAsync(new DicomRepositoryRepairResult());

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Completed, result.Status);

            var request = Assert.IsType<DicomRepositoryRepairRequest>(capturedRequest);
            Assert.True(request.VerifyFiles);
            Assert.False(request.RepairMissingFiles);
            Assert.Null(request.RepositoryLocationId);

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryCalledOnce();
        }

        private static CancellationToken CreateCanceledToken(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return cancellationToken;
            }

            var cancellationSource = new CancellationTokenSource();
            cancellationSource.Cancel();
            return cancellationSource.Token;
        }

        private static PersistenceIntegrityIssue CreatePersistenceIssue(PersistenceIntegrityIssueType issueType, PersistenceIntegrityEntityType entityType, int? entityId = null) => new()
        {
            DetectedAtUtc = DateTime.UtcNow,
            EntityId = entityId,
            EntityType = entityType,
            IssueType = issueType,
            PropertyName = "TestProperty",
            ReferencedEntityType = PersistenceIntegrityEntityType.Unknown,
            TechnicalDetails = "Technical test details",
            Value = "Technical test value"
        };
    }
}