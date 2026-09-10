using MarcusRunge.Mopr.Workbench.Contracts.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Enums;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;
using Moq;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Test
{
    public sealed class MirasFlowServiceTests
    {
        [Fact]
        public void NewFlow_HasExpectedIdleState()
        {
            using var context = new MirasServiceTestContext();

            Assert.Equal(MirasFlowState.Idle, context.Flow.CurrentState);
            Assert.False(context.Flow.IsRunning);
            Assert.True(context.Flow.CanStart);
            Assert.False(context.Flow.CanCancel);
            Assert.Null(context.Flow.LastResult);
            Assert.False(context.Flow.HasUnexpectedError);
        }

        [Theory]
        [InlineData(MirasOperationStatus.Completed)]
        [InlineData(MirasOperationStatus.CompletedWithIssues)]
        [InlineData(MirasOperationStatus.Blocked)]
        [InlineData(MirasOperationStatus.Incomplete)]
        [InlineData(MirasOperationStatus.Failed)]
        public async Task StartAsync_ReturnedMirasResult_CompletesFlow(MirasOperationStatus operationStatus)
        {
            using var context = new MirasServiceTestContext();

            ConfigureOperationResult(context, operationStatus);

            var actualResult = await context.Flow.StartAsync(TestContext.Current.CancellationToken);

            Assert.Equal(operationStatus, actualResult.Status);
            Assert.Same(actualResult, context.Flow.LastResult);
            Assert.Equal(MirasFlowState.Completed, context.Flow.CurrentState);
            Assert.False(context.Flow.IsRunning);
            Assert.True(context.Flow.CanStart);
            Assert.False(context.Flow.CanCancel);
            Assert.False(context.Flow.HasUnexpectedError);

            context.VerifyPersistenceCalledOnce();

            if (operationStatus is MirasOperationStatus.Completed or MirasOperationStatus.CompletedWithIssues or MirasOperationStatus.Incomplete)
            {
                context.VerifyRepositoryCalledOnce();
            }
            else
            {
                context.VerifyRepositoryNotCalled();
            }
        }

        [Fact]
        public async Task StartAsync_WhileRunning_ReturnsSameTaskAndStartsOneCheck()
        {
            using var context = new MirasServiceTestContext();
            var completion = new TaskCompletionSource<PersistenceIntegrityResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            context.PersistenceIntegrityService.Setup(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>())).Returns(completion.Task);
            context.ConfigureRepositoryResult(new DicomRepositoryRepairResult());

            var firstRun = context.Flow.StartAsync(TestContext.Current.CancellationToken);
            var secondRun = context.Flow.StartAsync(TestContext.Current.CancellationToken);

            Assert.Same(firstRun, secondRun);
            Assert.Equal(MirasFlowState.Running, context.Flow.CurrentState);
            Assert.True(context.Flow.IsRunning);
            Assert.False(context.Flow.CanStart);
            Assert.True(context.Flow.CanCancel);
            Assert.Null(context.Flow.LastResult);

            completion.SetResult(new PersistenceIntegrityResult());

            var firstResult = await firstRun;
            var secondResult = await secondRun;

            Assert.Same(firstResult, secondResult);
            Assert.Equal(MirasOperationStatus.Completed, firstResult.Status);
            Assert.Same(firstResult, context.Flow.LastResult);

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryCalledOnce();
        }

        [Fact]
        public async Task NewRun_ClearsPreviousResultWhileRunning()
        {
            using var context = new MirasServiceTestContext();
            var secondPersistenceCompletion = new TaskCompletionSource<PersistenceIntegrityResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            context.PersistenceIntegrityService.SetupSequence(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PersistenceIntegrityResult()).Returns(secondPersistenceCompletion.Task);
            context.RepositoryRepairService.SetupSequence(service => service.RepairAsync(It.IsAny<DicomRepositoryRepairRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new DicomRepositoryRepairResult()).ReturnsAsync(new DicomRepositoryRepairResult());

            var firstResult = await context.Flow.StartAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Completed, firstResult.Status);
            Assert.Same(firstResult, context.Flow.LastResult);

            var secondRun = context.Flow.StartAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasFlowState.Running, context.Flow.CurrentState);
            Assert.True(context.Flow.IsRunning);
            Assert.Null(context.Flow.LastResult);

            secondPersistenceCompletion.SetResult(new PersistenceIntegrityResult());

            var secondResult = await secondRun;

            Assert.Equal(MirasOperationStatus.Completed, secondResult.Status);
            Assert.NotSame(firstResult, secondResult);
            Assert.Same(secondResult, context.Flow.LastResult);

            context.PersistenceIntegrityService.Verify(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            context.RepositoryRepairService.Verify(service => service.RepairAsync(It.IsAny<DicomRepositoryRepairRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task SequentialRuns_UseIndependentResults()
        {
            using var context = new MirasServiceTestContext();
            var secondRepositoryResult = new DicomRepositoryRepairResult();
            secondRepositoryResult.Issues.Add(CreateRepositoryIssue(DicomRepositoryIssueType.MissingFile));

            context.PersistenceIntegrityService.SetupSequence(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PersistenceIntegrityResult()).ReturnsAsync(new PersistenceIntegrityResult());
            context.RepositoryRepairService.SetupSequence(service => service.RepairAsync(It.IsAny<DicomRepositoryRepairRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new DicomRepositoryRepairResult()).ReturnsAsync(secondRepositoryResult);

            var firstResult = await context.Flow.StartAsync(TestContext.Current.CancellationToken);
            var secondResult = await context.Flow.StartAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Completed, firstResult.Status);
            Assert.Equal(MirasOperationStatus.CompletedWithIssues, secondResult.Status);
            Assert.NotSame(firstResult, secondResult);
            Assert.Same(secondResult, context.Flow.LastResult);
            Assert.Equal(MirasFlowState.Completed, context.Flow.CurrentState);

            context.PersistenceIntegrityService.Verify(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            context.RepositoryRepairService.Verify(service => service.RepairAsync(It.IsAny<DicomRepositoryRepairRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task Cancel_WhileRunning_CancelsRunAndAllowsRestart()
        {
            using var context = new MirasServiceTestContext();
            var invocationCount = 0;
            var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            context.PersistenceIntegrityService.Setup(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>())).Returns<PersistenceIntegrityRequest, CancellationToken>(async (_, cancellationToken) =>
            {
                invocationCount++;

                if (invocationCount == 1)
                {
                    firstRunStarted.TrySetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }

                return new PersistenceIntegrityResult();
            });
            context.ConfigureRepositoryResult(new DicomRepositoryRepairResult());

            var firstRun = context.Flow.StartAsync(TestContext.Current.CancellationToken);

            await firstRunStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            context.Flow.Cancel();
            context.Flow.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await firstRun);

            Assert.Equal(MirasFlowState.Canceled, context.Flow.CurrentState);
            Assert.False(context.Flow.IsRunning);
            Assert.True(context.Flow.CanStart);
            Assert.False(context.Flow.CanCancel);
            Assert.Null(context.Flow.LastResult);
            Assert.False(context.Flow.HasUnexpectedError);

            var secondResult = await context.Flow.StartAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Completed, secondResult.Status);
            Assert.Same(secondResult, context.Flow.LastResult);
            Assert.Equal(2, invocationCount);

            context.PersistenceIntegrityService.Verify(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            context.VerifyRepositoryCalledOnce();
        }

        [Fact]
        public void Cancel_WhileIdle_IsIdempotent()
        {
            using var context = new MirasServiceTestContext();

            context.Flow.Cancel();
            context.Flow.Cancel();

            Assert.Equal(MirasFlowState.Idle, context.Flow.CurrentState);
            Assert.False(context.Flow.IsRunning);
            Assert.True(context.Flow.CanStart);
            Assert.False(context.Flow.CanCancel);
            Assert.Null(context.Flow.LastResult);
            Assert.False(context.Flow.HasUnexpectedError);
        }

        [Fact]
        public async Task CallerCancellation_CancelsRunWithoutUnexpectedError()
        {
            using var context = new MirasServiceTestContext();
            using var callerCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            var checkStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            context.PersistenceIntegrityService.Setup(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>())).Returns<PersistenceIntegrityRequest, CancellationToken>(async (_, cancellationToken) =>
            {
                checkStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new PersistenceIntegrityResult();
            });

            var run = context.Flow.StartAsync(callerCancellation.Token);

            await checkStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            callerCancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await run);

            Assert.Equal(MirasFlowState.Canceled, context.Flow.CurrentState);
            Assert.False(context.Flow.IsRunning);
            Assert.True(context.Flow.CanStart);
            Assert.False(context.Flow.CanCancel);
            Assert.False(context.Flow.HasUnexpectedError);
            Assert.Null(context.Flow.LastResult);

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryNotCalled();
        }

        [Fact]
        public async Task ApplicationStopping_CancelsRunAndPreventsRestart()
        {
            using var context = new MirasServiceTestContext();
            var checkStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            context.PersistenceIntegrityService.Setup(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>())).Returns<PersistenceIntegrityRequest, CancellationToken>(async (_, cancellationToken) =>
            {
                checkStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new PersistenceIntegrityResult();
            });

            var run = context.Flow.StartAsync(TestContext.Current.CancellationToken);

            await checkStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            context.ApplicationLifetime.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await run);

            Assert.Equal(MirasFlowState.Canceled, context.Flow.CurrentState);
            Assert.False(context.Flow.IsRunning);
            Assert.False(context.Flow.CanStart);
            Assert.False(context.Flow.CanCancel);
            Assert.Null(context.Flow.LastResult);
            Assert.False(context.Flow.HasUnexpectedError);

            var rejectedRun = context.Flow.StartAsync(TestContext.Current.CancellationToken);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await rejectedRun);

            context.VerifyPersistenceCalledOnce();
            context.VerifyRepositoryNotCalled();
        }

        [Fact]
        public async Task OperationsFailure_CompletesFlowWithFailedResultAndAllowsRestart()
        {
            using var context = new MirasServiceTestContext();
            var expectedException = new InvalidOperationException("Unexpected MIRAS test failure.");

            context.PersistenceIntegrityService.SetupSequence(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(expectedException).ReturnsAsync(new PersistenceIntegrityResult());
            context.ConfigureRepositoryResult(new DicomRepositoryRepairResult());

            var failedResult = await context.Flow.StartAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Failed, failedResult.Status);
            Assert.True(failedResult.HasTechnicalErrors);
            Assert.Same(failedResult, context.Flow.LastResult);
            Assert.Equal(MirasFlowState.Completed, context.Flow.CurrentState);
            Assert.False(context.Flow.IsRunning);
            Assert.True(context.Flow.CanStart);
            Assert.False(context.Flow.CanCancel);
            Assert.False(context.Flow.HasUnexpectedError);

            var successfulResult = await context.Flow.StartAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasOperationStatus.Completed, successfulResult.Status);
            Assert.Same(successfulResult, context.Flow.LastResult);
            Assert.Equal(MirasFlowState.Completed, context.Flow.CurrentState);
            Assert.False(context.Flow.HasUnexpectedError);

            context.PersistenceIntegrityService.Verify(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            context.VerifyRepositoryCalledOnce();
        }

        [Fact]
        public async Task StateChanges_RaiseBindingNotifications()
        {
            using var context = new MirasServiceTestContext();
            var completion = new TaskCompletionSource<PersistenceIntegrityResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var changedProperties = new HashSet<string?>();

            context.PersistenceIntegrityService.Setup(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>())).Returns(completion.Task);
            context.ConfigureRepositoryResult(new DicomRepositoryRepairResult());

            var run = context.Flow.StartAsync(TestContext.Current.CancellationToken);

            Assert.Contains(nameof(context.Flow.CurrentState), changedProperties);
            Assert.Contains(nameof(context.Flow.IsRunning), changedProperties);
            Assert.Contains(nameof(context.Flow.CanStart), changedProperties);
            Assert.Contains(nameof(context.Flow.CanCancel), changedProperties);
            Assert.Contains(nameof(context.Flow.LastResult), changedProperties);
            Assert.Contains(nameof(context.Flow.HasUnexpectedError), changedProperties);

            changedProperties.Clear();

            completion.SetResult(new PersistenceIntegrityResult());

            await run;

            Assert.Contains(nameof(context.Flow.CurrentState), changedProperties);
            Assert.Contains(nameof(context.Flow.IsRunning), changedProperties);
            Assert.Contains(nameof(context.Flow.CanStart), changedProperties);
            Assert.Contains(nameof(context.Flow.CanCancel), changedProperties);
            Assert.Contains(nameof(context.Flow.LastResult), changedProperties);
            Assert.Contains(nameof(context.Flow.HasUnexpectedError), changedProperties);
        }

        private static void ConfigureOperationResult(MirasServiceTestContext context, MirasOperationStatus operationStatus)
        {
            switch (operationStatus)
            {
                case MirasOperationStatus.Completed:
                    context.ConfigurePersistenceResult(new PersistenceIntegrityResult());
                    context.ConfigureRepositoryResult(new DicomRepositoryRepairResult());
                    break;

                case MirasOperationStatus.CompletedWithIssues:
                    var repositoryResult = new DicomRepositoryRepairResult();
                    repositoryResult.Issues.Add(CreateRepositoryIssue(DicomRepositoryIssueType.MissingFile));
                    context.ConfigurePersistenceResult(new PersistenceIntegrityResult());
                    context.ConfigureRepositoryResult(repositoryResult);
                    break;

                case MirasOperationStatus.Blocked:
                    var blockedPersistenceResult = new PersistenceIntegrityResult();
                    blockedPersistenceResult.Issues.Add(CreatePersistenceIssue(PersistenceIntegrityIssueType.MissingParent));
                    context.ConfigurePersistenceResult(blockedPersistenceResult);
                    break;

                case MirasOperationStatus.Incomplete:
                    var incompleteRepositoryResult = new DicomRepositoryRepairResult();
                    incompleteRepositoryResult.Errors.Add("Repository inspection failed.");
                    context.ConfigurePersistenceResult(new PersistenceIntegrityResult());
                    context.ConfigureRepositoryResult(incompleteRepositoryResult);
                    break;

                case MirasOperationStatus.Failed:
                    context.PersistenceIntegrityService.Setup(service => service.VerifyAsync(It.IsAny<PersistenceIntegrityRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Unexpected MIRAS test failure."));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(operationStatus), operationStatus, "The MIRAS operation status is not supported by this test scenario.");
            }
        }

        private static PersistenceIntegrityIssue CreatePersistenceIssue(PersistenceIntegrityIssueType issueType) => new()
        {
            DetectedAtUtc = DateTime.UtcNow,
            EntityId = 42,
            EntityType = PersistenceIntegrityEntityType.Instance,
            IssueType = issueType,
            PropertyName = "TestProperty",
            ReferencedEntityType = PersistenceIntegrityEntityType.Unknown,
            TechnicalDetails = "Technical test details",
            Value = "Technical test value"
        };

        private static DicomRepositoryIssue CreateRepositoryIssue(DicomRepositoryIssueType issueType) => new()
        {
            CanResolveAutomatically = false,
            DetectedAtUtc = DateTime.UtcNow,
            IssueType = issueType,
            TechnicalDetails = "Technical test details"
        };
    }
}