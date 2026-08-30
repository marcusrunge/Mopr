using MarcusRunge.Mopr.Workbench.Contracts.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Test
{
    public sealed class MirasServiceEdgeCaseTests
    {
        [Fact]
        public async Task CheckRepositoryAsync_WhenRepositoryCheckIsCanceled_PropagatesCancellation()
        {
            using var context = new MirasServiceTestContext();
            context.ConfigurePersistenceResult(new PersistenceIntegrityResult());

            context.RepositoryRepairService.Setup(service => service.RepairAsync(It.IsAny<DicomRepositoryRepairRequest>(), It.IsAny<CancellationToken>())).Returns<DicomRepositoryRepairRequest, CancellationToken>((_, cancellationToken) => Task.FromCanceled<DicomRepositoryRepairResult>(CreateCanceledToken(cancellationToken)));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken));

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryCalledOnce();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WithRepositoryIssueAndTechnicalError_ReturnsIncompleteWithIssue()
        {
            using var context = new MirasServiceTestContext();
            var repositoryResult = new DicomRepositoryRepairResult
            {
                ScannedFiles = 4
            };

            repositoryResult.Issues.Add(CreateRepositoryIssue(DicomRepositoryIssueType.MissingFile));
            repositoryResult.Errors.Add("Technical repository verification error.");

            context.ConfigurePersistenceResult(new PersistenceIntegrityResult
            {
                ScannedEntities = 6
            });
            context.ConfigureRepositoryResult(repositoryResult);

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Incomplete, result.Status);
            Assert.Equal(MirasAlertLevel.Warning, result.HighestAlertLevel);
            Assert.Equal(10, result.ScannedItems);
            Assert.True(result.HasIssues);
            Assert.True(result.HasTechnicalErrors);
            Assert.True(result.HasActionRequired);
            var item = Assert.Single(result.Issues);
            Assert.Single(result.TechnicalErrors);
            Assert.Equal(MirasIssueType.MissingFile, item.IssueType);
            Assert.Equal(MirasAlertLevel.Caution, result.Issues[0].AlertLevel);

            Assert.All(result.Messages, message =>
            {
                Assert.DoesNotContain("Technical repository verification error", message.Description, StringComparison.Ordinal);

                Assert.Empty(message.TechnicalDetails);
            });

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryCalledOnce();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WithPersistenceIssueAndTechnicalError_ReturnsIncompleteAndBlocksRepository()
        {
            using var context = new MirasServiceTestContext();
            var persistenceResult = new PersistenceIntegrityResult
            {
                ScannedEntities = 5
            };

            persistenceResult.Issues.Add(CreatePersistenceIssue(
                PersistenceIntegrityIssueType.InvalidValue));

            persistenceResult.Errors.Add("Technical persistence verification error.");
            context.ConfigurePersistenceResult(persistenceResult);

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Incomplete, result.Status);
            Assert.Equal(MirasAlertLevel.Warning, result.HighestAlertLevel);
            Assert.Equal(5, result.ScannedItems);
            Assert.True(result.HasIssues);
            Assert.True(result.HasTechnicalErrors);
            Assert.True(result.HasActionRequired);
            var item = Assert.Single(result.Issues);
            Assert.Single(result.TechnicalErrors);
            Assert.Equal(
                MirasIssueType.PersistenceValueInvalid,
                item.IssueType);

            Assert.All(result.Messages, message =>
            {
                Assert.DoesNotContain("Technical persistence verification error", message.Description, StringComparison.Ordinal);

                Assert.Empty(message.TechnicalDetails);
            });

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryNotCalled();
        }

        [Fact]
        public async Task MirasFactory_AfterDisposedFactoryLifetime_CreatesIndependentService()
        {
            var firstContext = new MirasServiceTestContext();
            firstContext.Dispose();

            using var secondContext = new MirasServiceTestContext();
            secondContext.ConfigurePersistenceResult(new PersistenceIntegrityResult
            {
                ScannedEntities = 2
            });
            secondContext.ConfigureRepositoryResult(new DicomRepositoryRepairResult
            {
                ScannedFiles = 3
            });

            var result = await secondContext.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Completed, result.Status);
            Assert.Equal(MirasAlertLevel.Normal, result.HighestAlertLevel);
            Assert.Equal(5, result.ScannedItems);
            Assert.False(result.HasIssues);
            Assert.False(result.HasTechnicalErrors);

            secondContext.VerifyPersistenceCalledOnce();
            secondContext.VerifyRepositoryCalledOnce();
        }

        [Fact]
        public async Task CheckRepositoryAsync_WhenCalledTwice_ExecutesCompleteInspectionForEachCall()
        {
            using var context = new MirasServiceTestContext();
            context.ConfigurePersistenceResult(new PersistenceIntegrityResult
            {
                ScannedEntities = 7
            });
            context.ConfigureRepositoryResult(new DicomRepositoryRepairResult
            {
                ScannedFiles = 11
            });

            var firstResult = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            var secondResult = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.NotSame(firstResult, secondResult);
            Assert.Equal(MirasOperationStatus.Completed, firstResult.Status);
            Assert.Equal(MirasOperationStatus.Completed, secondResult.Status);
            Assert.Equal(18, firstResult.ScannedItems);
            Assert.Equal(18, secondResult.ScannedItems);
            Assert.Single(firstResult.Messages);
            Assert.Single(secondResult.Messages);

            context.PersistenceIntegrityService.Verify(service => service.VerifyAsync(It.Is<PersistenceIntegrityRequest>(request => request.VerifyAuditReferences && request.VerifyRelationships && request.VerifyRequiredValues && request.VerifyUniqueValues), It.IsAny<CancellationToken>()), Times.Exactly(2));

            context.RepositoryRepairService.Verify(service => service.RepairAsync(It.Is<DicomRepositoryRepairRequest>(request => request.VerifyFiles && !request.RepairMissingFiles && request.RepositoryLocationId == null), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task CheckRepositoryAsync_PassesCancelableTokenToBothBackendServices()
        {
            using var context = new MirasServiceTestContext();
            CancellationToken persistenceToken = default;
            CancellationToken repositoryToken = default;

            context.PersistenceIntegrityService.Setup(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>())).Callback<PersistenceIntegrityRequest, CancellationToken>((_, cancellationToken) => persistenceToken = cancellationToken).ReturnsAsync(new PersistenceIntegrityResult());

            context.RepositoryRepairService.Setup(service => service.RepairAsync(It.IsAny<DicomRepositoryRepairRequest>(), It.IsAny<CancellationToken>())).Callback<DicomRepositoryRepairRequest, CancellationToken>((_, cancellationToken) => repositoryToken = cancellationToken).ReturnsAsync(new DicomRepositoryRepairResult());

            var result = await context.Service.CheckRepositoryAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Completed, result.Status);
            Assert.True(persistenceToken.CanBeCanceled);
            Assert.True(repositoryToken.CanBeCanceled);
            Assert.Equal(persistenceToken, repositoryToken);

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryCalledOnce();
        }

        private static CancellationToken CreateCanceledToken(
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return cancellationToken;
            }

            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, TestContext.Current.CancellationToken);

            cancellationSource.Cancel();
            return cancellationSource.Token;
        }

        private static PersistenceIntegrityIssue CreatePersistenceIssue(
            PersistenceIntegrityIssueType issueType) =>
            new()
            {
                DetectedAtUtc = DateTime.UtcNow,
                EntityId = 12,
                EntityType = PersistenceIntegrityEntityType.Instance,
                IssueType = issueType,
                PropertyName = "TechnicalProperty",
                ReferencedEntityId = 8,
                ReferencedEntityType = PersistenceIntegrityEntityType.Series,
                TechnicalDetails = "Technical persistence details",
                Value = "TechnicalValue"
            };

        private static DicomRepositoryIssue CreateRepositoryIssue(
            DicomRepositoryIssueType issueType) =>
            new()
            {
                ActualFilePath = @"C:\Sensitive\actual-image.dcm",
                ActualSopInstanceUid = "1.2.840.actual.instance",
                AutomaticallyResolved = false,
                CanResolveAutomatically = false,
                DetectedAtUtc = DateTime.UtcNow,
                ExpectedFilePath = @"C:\Sensitive\expected-image.dcm",
                ExpectedSopInstanceUid = "1.2.840.expected.instance",
                InstanceId = 31,
                IssueType = issueType,
                RepositoryLocationId = 7,
                TechnicalDetails = "Technical repository details"
            };
    }
}