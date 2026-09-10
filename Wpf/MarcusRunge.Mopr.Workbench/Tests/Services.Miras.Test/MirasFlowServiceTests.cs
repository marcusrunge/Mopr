using MarcusRunge.Mopr.Workbench.Contracts.Miras.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Miras.Models;
using Moq;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Test
{
    public sealed class MirasFlowServiceTests
    {
        [Fact]
        public void NewFlow_HasExpectedIdleState()
        {
            using var context = new MirasFlowServiceTestContext();

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
        public async Task StartAsync_ReturnedMirasResult_CompletesFlow(
            MirasOperationStatus operationStatus)
        {
            using var context = new MirasFlowServiceTestContext();
            var expectedResult = new MirasOperationResult
            {
                Status = operationStatus
            };

            context.MirasService.Setup(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);

            var actualResult = await context.Flow.StartAsync(TestContext.Current.CancellationToken);

            Assert.Same(expectedResult, actualResult);
            Assert.Same(expectedResult, context.Flow.LastResult);
            Assert.Equal(MirasFlowState.Completed, context.Flow.CurrentState);
            Assert.False(context.Flow.IsRunning);
            Assert.True(context.Flow.CanStart);
            Assert.False(context.Flow.CanCancel);
            Assert.False(context.Flow.HasUnexpectedError);

            context.MirasService.Verify(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StartAsync_WhileRunning_ReturnsSameTaskAndStartsOneCheck()
        {
            using var context = new MirasFlowServiceTestContext();
            var completion = new TaskCompletionSource<MirasOperationResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            context.MirasService.Setup(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>())).Returns(completion.Task);

            var firstRun = context.Flow.StartAsync(TestContext.Current.CancellationToken);
            var secondRun = context.Flow.StartAsync(TestContext.Current.CancellationToken);

            Assert.Same(firstRun, secondRun);
            Assert.Equal(MirasFlowState.Running, context.Flow.CurrentState);
            Assert.True(context.Flow.IsRunning);
            Assert.False(context.Flow.CanStart);
            Assert.True(context.Flow.CanCancel);
            Assert.Null(context.Flow.LastResult);

            var expectedResult = new MirasOperationResult
            {
                Status = MirasOperationStatus.Completed
            };

            completion.SetResult(expectedResult);

            Assert.Same(expectedResult, await firstRun);
            Assert.Same(expectedResult, await secondRun);

            context.MirasService.Verify(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task NewRun_ClearsPreviousResultWhileRunning()
        {
            using var context = new MirasFlowServiceTestContext();
            var firstResult = new MirasOperationResult
            {
                Status = MirasOperationStatus.Completed
            };
            var secondCompletion = new TaskCompletionSource<MirasOperationResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            context.MirasService.SetupSequence(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(firstResult).Returns(secondCompletion.Task);

            Assert.Same(firstResult, await context.Flow.StartAsync(TestContext.Current.CancellationToken));
            Assert.Same(firstResult, context.Flow.LastResult);

            var secondRun = context.Flow.StartAsync(TestContext.Current.CancellationToken);

            Assert.Equal(MirasFlowState.Running, context.Flow.CurrentState);
            Assert.True(context.Flow.IsRunning);
            Assert.Null(context.Flow.LastResult);

            var secondResult = new MirasOperationResult
            {
                Status = MirasOperationStatus.Blocked
            };

            secondCompletion.SetResult(secondResult);

            Assert.Same(secondResult, await secondRun);
            Assert.Same(secondResult, context.Flow.LastResult);
        }

        [Fact]
        public async Task SequentialRuns_UseIndependentResults()
        {
            using var context = new MirasFlowServiceTestContext();
            var firstResult = new MirasOperationResult
            {
                Status = MirasOperationStatus.CompletedWithIssues
            };
            var secondResult = new MirasOperationResult
            {
                Status = MirasOperationStatus.Completed
            };

            context.MirasService.SetupSequence(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(firstResult).ReturnsAsync(secondResult);

            var actualFirstResult = await context.Flow.StartAsync(TestContext.Current.CancellationToken);
            var actualSecondResult = await context.Flow.StartAsync(TestContext.Current.CancellationToken);

            Assert.Same(firstResult, actualFirstResult);
            Assert.Same(secondResult, actualSecondResult);
            Assert.Same(secondResult, context.Flow.LastResult);
            Assert.Equal(MirasFlowState.Completed, context.Flow.CurrentState);

            context.MirasService.Verify(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task Cancel_WhileRunning_CancelsRunAndAllowsRestart()
        {
            using var context = new MirasFlowServiceTestContext();
            var invocationCount = 0;
            var firstRunStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondResult = new MirasOperationResult
            {
                Status = MirasOperationStatus.Completed
            };

            context.MirasService.Setup(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>())).Returns<CancellationToken>(async cancellationToken =>
            {
                invocationCount++;

                if (invocationCount == 1)
                {
                    firstRunStarted.TrySetResult();

                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }

                return secondResult;
            });

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

            var actualSecondResult = await context.Flow.StartAsync(TestContext.Current.CancellationToken);

            Assert.Same(secondResult, actualSecondResult);
            Assert.Equal(2, invocationCount);
        }

        [Fact]
        public void Cancel_WhileIdle_IsIdempotent()
        {
            using var context = new MirasFlowServiceTestContext();

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
            using var context = new MirasFlowServiceTestContext();
            using var callerCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            var checkStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            context.MirasService.Setup(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>())).Returns<CancellationToken>(async cancellationToken =>
            {
                checkStarted.TrySetResult();

                await Task.Delay(
                    Timeout.InfiniteTimeSpan,
                    cancellationToken);

                return new MirasOperationResult();
            });

            var run = context.Flow.StartAsync(callerCancellation.Token);

            await checkStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            callerCancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await run);

            Assert.Equal(MirasFlowState.Canceled, context.Flow.CurrentState);
            Assert.False(context.Flow.HasUnexpectedError);
            Assert.Null(context.Flow.LastResult);
            Assert.True(context.Flow.CanStart);
        }

        [Fact]
        public async Task ApplicationStopping_CancelsRunAndPreventsRestart()
        {
            using var context = new MirasFlowServiceTestContext();
            var checkStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            context.MirasService.Setup(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>())).Returns<CancellationToken>(async cancellationToken =>
            {
                checkStarted.TrySetResult();

                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

                return new MirasOperationResult();
            });

            var run = context.Flow.StartAsync(TestContext.Current.CancellationToken);

            await checkStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            context.ApplicationLifetime.Stop();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await run);

            Assert.Equal(MirasFlowState.Canceled, context.Flow.CurrentState);
            Assert.False(context.Flow.IsRunning);
            Assert.False(context.Flow.CanStart);
            Assert.False(context.Flow.CanCancel);
            Assert.Null(context.Flow.LastResult);
            Assert.False(context.Flow.HasUnexpectedError);

            var rejectedRun = context.Flow.StartAsync(TestContext.Current.CancellationToken);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await rejectedRun);

            context.MirasService.Verify(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UnexpectedException_SetsFailedAndAllowsRestart()
        {
            using var context = new MirasFlowServiceTestContext();
            var expectedException = new InvalidOperationException("Unexpected MIRAS test failure.");
            var successfulResult = new MirasOperationResult
            {
                Status = MirasOperationStatus.Completed
            };

            context.MirasService.SetupSequence(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>())).ThrowsAsync(expectedException).ReturnsAsync(successfulResult);

            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () => await context.Flow.StartAsync(TestContext.Current.CancellationToken));

            Assert.Same(expectedException, actualException);
            Assert.Equal(MirasFlowState.Failed, context.Flow.CurrentState);
            Assert.False(context.Flow.IsRunning);
            Assert.True(context.Flow.CanStart);
            Assert.False(context.Flow.CanCancel);
            Assert.Null(context.Flow.LastResult);
            Assert.True(context.Flow.HasUnexpectedError);

            var actualResult = await context.Flow.StartAsync(TestContext.Current.CancellationToken);

            Assert.Same(successfulResult, actualResult);
            Assert.Same(successfulResult, context.Flow.LastResult);
            Assert.Equal(MirasFlowState.Completed, context.Flow.CurrentState);
            Assert.False(context.Flow.HasUnexpectedError);
        }

        [Fact]
        public async Task StateChanges_RaiseBindingNotifications()
        {
            using var context = new MirasFlowServiceTestContext();
            var completion = new TaskCompletionSource<MirasOperationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var changedProperties = new HashSet<string?>();

            context.MirasService.Setup(service => service.CheckRepositoryAsync(It.IsAny<CancellationToken>())).Returns(completion.Task);

            context.Flow.PropertyChanged += OnPropertyChanged;

            try
            {
                var run = context.Flow.StartAsync(TestContext.Current.CancellationToken);

                Assert.Contains(nameof(context.Flow.CurrentState), changedProperties);
                Assert.Contains(nameof(context.Flow.IsRunning), changedProperties);
                Assert.Contains(nameof(context.Flow.CanStart), changedProperties);
                Assert.Contains(nameof(context.Flow.CanCancel), changedProperties);
                Assert.Contains(nameof(context.Flow.LastResult), changedProperties);
                Assert.Contains(nameof(context.Flow.HasUnexpectedError), changedProperties);

                changedProperties.Clear();

                completion.SetResult(new MirasOperationResult
                {
                    Status = MirasOperationStatus.Completed
                });

                await run;

                Assert.Contains(nameof(context.Flow.CurrentState), changedProperties);
                Assert.Contains(nameof(context.Flow.IsRunning), changedProperties);
                Assert.Contains(nameof(context.Flow.CanStart), changedProperties);
                Assert.Contains(nameof(context.Flow.CanCancel), changedProperties);
                Assert.Contains(nameof(context.Flow.LastResult), changedProperties);
                Assert.Contains(nameof(context.Flow.HasUnexpectedError), changedProperties);
            }
            finally
            {
                context.Flow.PropertyChanged -= OnPropertyChanged;
            }

            void OnPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs) => changedProperties.Add(eventArgs.PropertyName);
        }
    }
}