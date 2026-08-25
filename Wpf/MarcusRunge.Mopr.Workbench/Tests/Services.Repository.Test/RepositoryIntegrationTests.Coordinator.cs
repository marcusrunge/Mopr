using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    public sealed partial class RepositoryIntegrationTests
    {
        [Fact, Priority(64)]
        public async Task Import_Should_Wait_For_Repair_Lease_Of_Same_RepositoryLocation()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            IRepositoryOperationsCoordinator coordinator = GetOperationsCoordinator();
            IAsyncDisposable repairLease = await coordinator.AcquireRepairAsync(
                scenario.RepositoryLocation.Id,
                TestContext.Current.CancellationToken);

            try
            {
                /*
                 * The real Import service must use the same coordinator instance as
                 * Repair. Holding the location's repair lease therefore prevents the
                 * import from reaching any physical or Persistence mutation.
                 */
                Task<DicomImportResult> importTask = ImportAsync(scenario.SourceDirectory, repositoryLocationId: scenario.RepositoryLocation.Id);

                Assert.False(importTask.IsCompleted);
                Assert.False(File.Exists(scenario.PathInfo.AbsolutePath));
                AssertNoImportArtifacts(scenario.PathInfo);
                Assert.Null(await scenario.TryGetPersistedInstanceAsync());

                await repairLease.DisposeAsync();

                DicomImportResult result = await importTask;

                Assert.Equal(1, result.ImportedFiles);
                Assert.Equal(0, result.SkippedFiles);
                Assert.Equal(0, result.FailedFiles);
                Assert.Empty(result.Errors);
                Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));
                Assert.NotNull(await scenario.TryGetPersistedInstanceAsync());
                AssertNoImportArtifacts(scenario.PathInfo);
            }
            finally
            {
                await repairLease.DisposeAsync();
            }
        }

        [Fact, Priority(65)]
        public async Task Repair_Should_Wait_For_Import_Lease_Of_Same_RepositoryLocation()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            await scenario.ImportSuccessfullyAsync();

            IRepositoryOperationsCoordinator coordinator = GetOperationsCoordinator();
            IAsyncDisposable importLease = await coordinator.AcquireImportAsync(scenario.RepositoryLocation.Id, scenario.PathInfo.AbsolutePath, TestContext.Current.CancellationToken);

            try
            {
                /*
                 * The manually held lease represents an import inside its protected
                 * file-system and Persistence interval. The real Repair service must
                 * wait for that interval to finish before indexing the location.
                 */
                Task<DicomRepositoryRepairResult> repairTask = RepairAsync(CreateRepairRequest(repairMissingFiles: false));

                Assert.False(repairTask.IsCompleted);

                await importLease.DisposeAsync();

                DicomRepositoryRepairResult result = await repairTask;

                Assert.DoesNotContain(result.Issues, issue => issue.RepositoryLocationId == scenario.RepositoryLocation.Id && issue.ExpectedSopInstanceUid == scenario.SopInstanceUid.UID);

                Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));
                Assert.NotNull(await scenario.TryGetPersistedInstanceAsync());
                AssertNoImportArtifacts(scenario.PathInfo);
            }
            finally
            {
                await importLease.DisposeAsync();
            }
        }

        [Fact, Priority(66)]
        public async Task Repair_Should_Be_Serialized_For_Same_RepositoryLocation()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            await scenario.ImportSuccessfullyAsync();

            IRepositoryOperationsCoordinator coordinator = GetOperationsCoordinator();
            IAsyncDisposable blockingRepairLease = await coordinator.AcquireRepairAsync(scenario.RepositoryLocation.Id, TestContext.Current.CancellationToken);

            try
            {
                Task<DicomRepositoryRepairResult> firstRepairTask = RepairAsync(
                    CreateRepairRequest(repairMissingFiles: false));

                Task<DicomRepositoryRepairResult> secondRepairTask = RepairAsync(
                    CreateRepairRequest(repairMissingFiles: false));

                /*
                 * Both real Repair operations target the same location and must wait
                 * while an exclusive repair lease is already active. After release,
                 * the coordinator permits only one Repair at a time.
                 */
                Assert.False(firstRepairTask.IsCompleted);
                Assert.False(secondRepairTask.IsCompleted);

                await blockingRepairLease.DisposeAsync();

                DicomRepositoryRepairResult[] results = await Task.WhenAll(
                    firstRepairTask,
                    secondRepairTask);

                Assert.All(results, result => Assert.DoesNotContain(result.Issues, issue => issue.RepositoryLocationId == scenario.RepositoryLocation.Id && issue.ExpectedSopInstanceUid == scenario.SopInstanceUid.UID));

                Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));
                Assert.NotNull(await scenario.TryGetPersistedInstanceAsync());
                AssertNoImportArtifacts(scenario.PathInfo);
            }
            finally
            {
                await blockingRepairLease.DisposeAsync();
            }
        }

        [Fact, Priority(67)]
        public async Task Import_Should_Remain_Parallel_For_Different_RepositoryLocations()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync(repositoryLocation: _fixture.SecondaryRepositoryLocation!);

            IRepositoryOperationsCoordinator coordinator = GetOperationsCoordinator();

            /*
             * An exclusive operation in the primary location must not block an import
             * targeting the independent secondary location.
             */
            await using IAsyncDisposable primaryRepairLease = await coordinator.AcquireRepairAsync(_fixture.RepositoryLocation!.Id, TestContext.Current.CancellationToken);

            DicomImportResult result = await ImportAsync(
                scenario.SourceDirectory,
                repositoryLocationId: scenario.RepositoryLocation.Id);

            Assert.Equal(1, result.ImportedFiles);
            Assert.Equal(0, result.SkippedFiles);
            Assert.Equal(0, result.FailedFiles);
            Assert.Empty(result.Errors);
            Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));

            string primaryPath = _fixture.Repository!.RepositoryService!.GetAbsolutePath(_fixture.RepositoryLocation, scenario.PathInfo.RelativePath);

            Assert.False(File.Exists(primaryPath));
            Assert.NotNull(await scenario.TryGetPersistedInstanceAsync());
            AssertNoImportArtifacts(scenario.PathInfo);
        }

        [Fact, Priority(68)]
        public async Task Import_Cancellation_While_Waiting_For_Repair_Should_Be_Propagated()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            IRepositoryOperationsCoordinator coordinator = GetOperationsCoordinator();

            await using IAsyncDisposable repairLease = await coordinator.AcquireRepairAsync(scenario.RepositoryLocation.Id, TestContext.Current.CancellationToken);

            using CancellationTokenSource cancellationTokenSource = new();

            Task<DicomImportResult> importTask = _fixture.Repository!.ImportService!.ImportAsync(new DicomImportRequest
            {
                SourcePath = scenario.SourceDirectory,
                SourceType = ImportSourceType.Directory,
                RepositoryLocationId = scenario.RepositoryLocation.Id,
                AllowOverwrite = false,
                CreatedByUserId = _fixture.TestUser!.Id
            },
                cancellationTokenSource.Token);

            Assert.False(importTask.IsCompleted);

            await cancellationTokenSource.CancelAsync();

            /*
             * Cancellation while waiting for the coordinator is control flow, not a
             * normal per-file import failure. OperationCanceledException must leave
             * the service unchanged.
             */
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await importTask);

            Assert.False(File.Exists(scenario.PathInfo.AbsolutePath));
            Assert.Null(await scenario.TryGetPersistedInstanceAsync());
            AssertNoImportArtifacts(scenario.PathInfo);
        }

        [Fact, Priority(69)]
        public async Task RepositoryLocation_Should_Remain_Usable_After_Cancelled_Import_Wait()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            IRepositoryOperationsCoordinator coordinator = GetOperationsCoordinator();
            IAsyncDisposable repairLease = await coordinator.AcquireRepairAsync(scenario.RepositoryLocation.Id, TestContext.Current.CancellationToken);

            try
            {
                using CancellationTokenSource cancellationTokenSource = new();

                Task<DicomImportResult> cancelledImportTask = _fixture.Repository!.ImportService!.ImportAsync(
                    new DicomImportRequest
                    {
                        SourcePath = scenario.SourceDirectory,
                        SourceType = ImportSourceType.Directory,
                        RepositoryLocationId = scenario.RepositoryLocation.Id,
                        AllowOverwrite = false,
                        CreatedByUserId = _fixture.TestUser!.Id
                    },
                    cancellationTokenSource.Token);

                Assert.False(cancelledImportTask.IsCompleted);

                await cancellationTokenSource.CancelAsync();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await cancelledImportTask);

                await repairLease.DisposeAsync();

                /*
                 * A cancelled waiter must not consume or leak a semaphore permit.
                 * The next real Import must acquire the same location and destination
                 * normally.
                 */
                DicomImportResult result = await ImportAsync(scenario.SourceDirectory, repositoryLocationId: scenario.RepositoryLocation.Id);

                Assert.Equal(1, result.ImportedFiles);
                Assert.Equal(0, result.SkippedFiles);
                Assert.Equal(0, result.FailedFiles);
                Assert.Empty(result.Errors);
                Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));
                Assert.NotNull(await scenario.TryGetPersistedInstanceAsync());
                AssertNoImportArtifacts(scenario.PathInfo);
            }
            finally
            {
                await repairLease.DisposeAsync();
            }
        }

        [Fact, Priority(70)]
        public async Task Repair_Should_Not_Report_Import_Intermediate_State()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            IRepositoryOperationsCoordinator coordinator = GetOperationsCoordinator();
            IAsyncDisposable repairLease = await coordinator.AcquireRepairAsync(scenario.RepositoryLocation.Id, TestContext.Current.CancellationToken);

            try
            {
                /*
                 * The real Import is started while Repair owns the location. It must
                 * remain outside every physical and Persistence mutation until the
                 * exclusive lease is released.
                 */
                Task<DicomImportResult> importTask = ImportAsync(scenario.SourceDirectory, repositoryLocationId: scenario.RepositoryLocation.Id);

                Assert.False(importTask.IsCompleted);
                Assert.False(File.Exists(scenario.PathInfo.AbsolutePath));
                AssertNoImportArtifacts(scenario.PathInfo);
                Assert.Null(await scenario.TryGetPersistedInstanceAsync());

                await repairLease.DisposeAsync();

                DicomImportResult importResult = await importTask;

                Assert.Equal(1, importResult.ImportedFiles);
                Assert.Equal(0, importResult.FailedFiles);
                Assert.Empty(importResult.Errors);
                AssertNoImportArtifacts(scenario.PathInfo);

                DicomRepositoryRepairResult repairResult = await RepairAsync(new DicomRepositoryRepairRequest
                {
                    VerifyFiles = true,
                    RepositoryLocationId = scenario.RepositoryLocation.Id
                });

                /*
                 * The completed import must appear atomically to Repair. No temporary
                 * artifact or split file/Persistence state may produce a false issue.
                 */
                Assert.DoesNotContain(repairResult.Issues, issue => issue.RepositoryLocationId == scenario.RepositoryLocation.Id && issue.ExpectedSopInstanceUid == scenario.SopInstanceUid.UID && issue.IssueType is DicomRepositoryIssueType.MissingFile or DicomRepositoryIssueType.DuplicateFile or DicomRepositoryIssueType.OrphanedFile or DicomRepositoryIssueType.IncompleteImport);

                Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));
                Assert.NotNull(await scenario.TryGetPersistedInstanceAsync());
                AssertNoImportArtifacts(scenario.PathInfo);
            }
            finally
            {
                await repairLease.DisposeAsync();
            }
        }

        [Fact, Priority(71)]
        public async Task Repair_All_Should_Complete_Without_Holding_Multiple_Location_Locks()
        {
            using RepositoryTestScenario primaryScenario = await CreateRepositoryScenarioAsync();
            using RepositoryTestScenario secondaryScenario = await CreateRepositoryScenarioAsync(repositoryLocation: _fixture.SecondaryRepositoryLocation!);

            await primaryScenario.ImportSuccessfullyAsync();
            await secondaryScenario.ImportSuccessfullyAsync();

            /*
             * The all-locations Repair deliberately acquires and releases one location
             * at a time. It therefore completes without depending on a global
             * multi-location lock order.
             */
            DicomRepositoryRepairResult result = await RepairAsync(CreateAllLocationsRepairRequest(repairMissingFiles: false));

            Assert.DoesNotContain(result.Issues, issue => issue.ExpectedSopInstanceUid == primaryScenario.SopInstanceUid.UID || issue.ExpectedSopInstanceUid == secondaryScenario.SopInstanceUid.UID);

            Assert.True(File.Exists(primaryScenario.PathInfo.AbsolutePath));
            Assert.True(File.Exists(secondaryScenario.PathInfo.AbsolutePath));
            AssertNoImportArtifacts(primaryScenario.PathInfo);
            AssertNoImportArtifacts(secondaryScenario.PathInfo);
        }

        [Fact, Priority(72)]
        public async Task Repository_Should_Remain_Operational_After_Coordinator_Integration_Tests()
        {
            using RepositoryTestScenario primaryScenario = await CreateRepositoryScenarioAsync();
            using RepositoryTestScenario secondaryScenario = await CreateRepositoryScenarioAsync(repositoryLocation: _fixture.SecondaryRepositoryLocation!);

            DicomImportResult primaryImport = await primaryScenario.ImportSuccessfullyAsync();
            DicomImportResult secondaryImport = await secondaryScenario.ImportSuccessfullyAsync();

            Assert.Equal(1, primaryImport.ImportedFiles);
            Assert.Equal(1, secondaryImport.ImportedFiles);

            DicomRepositoryRepairResult repairResult = await RepairAsync(CreateAllLocationsRepairRequest(repairMissingFiles: false));

            Assert.DoesNotContain(repairResult.Issues, issue => issue.ExpectedSopInstanceUid == primaryScenario.SopInstanceUid.UID || issue.ExpectedSopInstanceUid == secondaryScenario.SopInstanceUid.UID);

            Assert.True(File.Exists(primaryScenario.PathInfo.AbsolutePath));
            Assert.True(File.Exists(secondaryScenario.PathInfo.AbsolutePath));
            Assert.NotNull(await primaryScenario.TryGetPersistedInstanceAsync());
            Assert.NotNull(await secondaryScenario.TryGetPersistedInstanceAsync());
            AssertNoImportArtifacts(primaryScenario.PathInfo);
            AssertNoImportArtifacts(secondaryScenario.PathInfo);
        }

        [Fact, Priority(73)]
        public async Task Import_Lock_Should_Be_Released_When_Aggregated_Import_And_Compensation_Error_Leaves_Lease_Scope()
        {
            using RepositoryTestScenario scenario = await CreateRepositoryScenarioAsync();
            IRepositoryOperationsCoordinator coordinator = GetOperationsCoordinator();

            AggregateException exception = await Assert.ThrowsAsync<AggregateException>(async () =>
            {
                await using IAsyncDisposable importLease = await coordinator.AcquireImportAsync(scenario.RepositoryLocation.Id, scenario.PathInfo.AbsolutePath, TestContext.Current.CancellationToken);

                /*
                 * The Coordinator does not own the file-system compensation itself.
                 * Its invariant is that even an aggregated operation and compensation
                 * failure leaving the protected import scope releases both hierarchy
                 * levels without masking either original failure.
                 */
                throw new AggregateException("The controlled import and compensation operation failed.", new IOException("Controlled import failure."), new IOException("Controlled compensation failure."));
            });

            Assert.Collection(exception.InnerExceptions, importException => Assert.Equal("Controlled import failure.", importException.Message), compensationException => Assert.Equal("Controlled compensation failure.", compensationException.Message));

            /*
             * A real import of the same location and canonical target proves that
             * neither the location reader nor the destination semaphore leaked.
             */
            DicomImportResult result = await ImportAsync(scenario.SourceDirectory, repositoryLocationId: scenario.RepositoryLocation.Id);

            Assert.Equal(1, result.ImportedFiles);
            Assert.Equal(0, result.SkippedFiles);
            Assert.Equal(0, result.FailedFiles);
            Assert.Empty(result.Errors);
            Assert.True(File.Exists(scenario.PathInfo.AbsolutePath));
            Assert.NotNull(await scenario.TryGetPersistedInstanceAsync());
            AssertNoImportArtifacts(scenario.PathInfo);
        }

        private IRepositoryOperationsCoordinator GetOperationsCoordinator()
        {
            IRepositoryBase repositoryBase = Assert.IsType<IRepositoryBase>(_fixture.Repository, exactMatch: false);

            return repositoryBase.OperationsCoordinator ?? throw new InvalidOperationException("The repository operations coordinator has not been initialized.");
        }
    }
}