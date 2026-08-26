using MarcusRunge.Mopr.Workbench.Application.Diagnostics;
using MarcusRunge.Mopr.Workbench.Application.SingleInstance;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test
{
    public sealed class SingleInstanceCoordinatorFixture
    {
        [Fact]
        public async Task TryBecomePrimaryInstance_FirstCoordinator_ReturnsPrimaryInstance()
        {
            var options = CreateOptions();
            await using var coordinator = CreateCoordinator(options);

            var result = coordinator.TryBecomePrimaryInstance();

            Assert.Equal(SingleInstanceStartResult.PrimaryInstance, result);
        }

        [Fact]
        public async Task TryBecomePrimaryInstance_SecondCoordinator_ReturnsSecondaryInstance()
        {
            var options = CreateOptions();
            await using var primaryCoordinator = CreateCoordinator(options);
            await using var secondaryCoordinator = CreateCoordinator(options);

            var primaryResult = primaryCoordinator.TryBecomePrimaryInstance();
            var secondaryResult = secondaryCoordinator.TryBecomePrimaryInstance();

            Assert.Equal(SingleInstanceStartResult.PrimaryInstance, primaryResult);
            Assert.Equal(SingleInstanceStartResult.SecondaryInstance, secondaryResult);
        }

        [Fact]
        public async Task TryBecomePrimaryInstance_DifferentNames_DoNotBlockEachOther()
        {
            await using var firstCoordinator = CreateCoordinator(CreateOptions());
            await using var secondCoordinator = CreateCoordinator(CreateOptions());

            var firstResult = firstCoordinator.TryBecomePrimaryInstance();
            var secondResult = secondCoordinator.TryBecomePrimaryInstance();

            Assert.Equal(SingleInstanceStartResult.PrimaryInstance, firstResult);
            Assert.Equal(SingleInstanceStartResult.PrimaryInstance, secondResult);
        }

        [Fact]
        public async Task TryBecomePrimaryInstance_AbandonedMutex_IsRecovered()
        {
            var options = CreateOptions();
            CreateAbandonedMutex(options.MutexName);

            await using var coordinator = CreateCoordinator(options);

            var result = coordinator.TryBecomePrimaryInstance();

            Assert.Equal(SingleInstanceStartResult.PrimaryInstance, result);
        }

        [Fact]
        public async Task TryBecomePrimaryInstance_RepeatedAttempt_ThrowsInvalidOperationException()
        {
            var options = CreateOptions();
            await using var coordinator = CreateCoordinator(options);

            Assert.Equal(SingleInstanceStartResult.PrimaryInstance, coordinator.TryBecomePrimaryInstance());

            Assert.Throws<InvalidOperationException>(() => coordinator.TryBecomePrimaryInstance());
        }

        [Fact]
        public async Task ForwardToPrimaryInstanceAsync_ArgumentsAreTransferred()
        {
            var options = CreateOptions();
            var foregroundPermission = new TestForegroundPermission();
            var receivedRequest = new TaskCompletionSource<SingleInstanceRequest>(TaskCreationOptions.RunContinuationsAsynchronously);

            await using var primaryCoordinator = CreateCoordinator(options);
            await using var secondaryCoordinator = CreateCoordinator(options, foregroundPermission);

            Assert.Equal(SingleInstanceStartResult.PrimaryInstance, primaryCoordinator.TryBecomePrimaryInstance());

            primaryCoordinator.StartListening((request, _) =>
            {
                receivedRequest.TrySetResult(request);
                return Task.CompletedTask;
            });

            Assert.Equal(SingleInstanceStartResult.SecondaryInstance, secondaryCoordinator.TryBecomePrimaryInstance());

            var arguments = new[] { @"C:\Dicom\Study 1", "--import" };
            await secondaryCoordinator.ForwardToPrimaryInstanceAsync(arguments, CancellationToken.None);

            var request = await receivedRequest.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            Assert.Equal(arguments, request.Arguments);
            Assert.Equal(Environment.ProcessId, foregroundPermission.AllowedProcessId);
        }

        [Fact]
        public async Task DisposeAsync_ReleasesPrimaryInstanceMarker()
        {
            var options = CreateOptions();
            var primaryCoordinator = CreateCoordinator(options);

            Assert.Equal(SingleInstanceStartResult.PrimaryInstance, primaryCoordinator.TryBecomePrimaryInstance());

            await primaryCoordinator.DisposeAsync();

            await using var replacementCoordinator = CreateCoordinator(options);

            Assert.Equal(SingleInstanceStartResult.PrimaryInstance, replacementCoordinator.TryBecomePrimaryInstance());
        }

        [Fact]
        public async Task DisposeAsync_RepeatedCall_IsSafe()
        {
            var options = CreateOptions();
            var coordinator = CreateCoordinator(options);

            Assert.Equal(SingleInstanceStartResult.PrimaryInstance, coordinator.TryBecomePrimaryInstance());

            await coordinator.DisposeAsync();
            await coordinator.DisposeAsync();

            await using var replacementCoordinator = CreateCoordinator(options);

            Assert.Equal(SingleInstanceStartResult.PrimaryInstance, replacementCoordinator.TryBecomePrimaryInstance());
        }

        [Fact]
        public async Task DisposeAsync_SecondaryCoordinator_DoesNotReleasePrimaryInstanceMarker()
        {
            var options = CreateOptions();
            await using var primaryCoordinator = CreateCoordinator(options);
            var secondaryCoordinator = CreateCoordinator(options);

            Assert.Equal(SingleInstanceStartResult.PrimaryInstance, primaryCoordinator.TryBecomePrimaryInstance());
            Assert.Equal(SingleInstanceStartResult.SecondaryInstance, secondaryCoordinator.TryBecomePrimaryInstance());

            await secondaryCoordinator.DisposeAsync();

            await using var thirdCoordinator = CreateCoordinator(options);

            Assert.Equal(SingleInstanceStartResult.SecondaryInstance, thirdCoordinator.TryBecomePrimaryInstance());
        }

        [Fact]
        public async Task DisposeAsync_StopsWaitingServerWithoutReportingCancellationAsError()
        {
            var options = CreateOptions();
            var diagnostics = new TestStartupDiagnostics();

            await using var coordinator = CreateCoordinator(options, diagnostics: diagnostics);

            Assert.Equal(SingleInstanceStartResult.PrimaryInstance, coordinator.TryBecomePrimaryInstance());

            coordinator.StartListening((_, _) => Task.CompletedTask);

            await coordinator.DisposeAsync();

            Assert.DoesNotContain(diagnostics.Errors, entry => entry.Exception is OperationCanceledException);
        }

        private static SingleInstanceCoordinator CreateCoordinator(SingleInstanceOptions options, TestForegroundPermission? foregroundPermission = null, TestStartupDiagnostics? diagnostics = null) => new(options, diagnostics ?? new TestStartupDiagnostics(), foregroundPermission ?? new TestForegroundPermission());

        private static SingleInstanceOptions CreateOptions()
        {
            var identifier = Guid.NewGuid().ToString("N");

            return new SingleInstanceOptions
            {
                MutexName = $@"Local\MOPR.Workbench.Tests.{identifier}",
                PipeName = $"MOPR.Workbench.Tests.{identifier}",
                ClientConnectionTimeout = TimeSpan.FromSeconds(10)
            };
        }

        private static void CreateAbandonedMutex(string mutexName)
        {
            using var acquisitionCompleted = new ManualResetEventSlim(false);
            Exception? ownerException = null;

            var ownerThread = new Thread(() =>
            {
                try
                {
                    using var mutex = new Mutex(false, mutexName);
                    mutex.WaitOne();
                    acquisitionCompleted.Set();

                    // Returning without ReleaseMutex simulates a terminated process or thread that abandoned ownership.
                }
                catch (Exception exception)
                {
                    ownerException = exception;
                    acquisitionCompleted.Set();
                }
            })
            {
                IsBackground = true,
                Name = "MOPR abandoned mutex test owner"
            };

            ownerThread.Start();
            acquisitionCompleted.Wait();
            ownerThread.Join();

            if (ownerException is not null)
            {
                throw new InvalidOperationException("The abandoned mutex test could not establish ownership.", ownerException);
            }
        }

        private sealed class TestStartupDiagnostics : IStartupDiagnostics
        {
            private readonly Lock _synchronization = new();
            private readonly List<(string Message, Exception Exception)> _errors = [];

            public IReadOnlyList<(string Message, Exception Exception)> Errors
            {
                get
                {
                    lock (_synchronization)
                    {
                        return [.. _errors];
                    }
                }
            }

            public void WriteInformation(string message)
            {
                // Information entries are not relevant to these coordination tests.
            }

            public void WriteError(string message, Exception exception)
            {
                lock (_synchronization)
                {
                    _errors.Add((message, exception));
                }
            }
        }

        private sealed class TestForegroundPermission : IForegroundPermission
        {
            public int? AllowedProcessId { get; private set; }

            public void AllowPrimaryInstance(int processId) => AllowedProcessId = processId;
        }
    }
}