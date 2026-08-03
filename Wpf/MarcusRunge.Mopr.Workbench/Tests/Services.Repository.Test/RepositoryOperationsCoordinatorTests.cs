using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Implementations;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Test
{
    public sealed class RepositoryOperationsCoordinatorTests
    {
        [Fact]
        public async Task Import_Should_Wait_For_Repair_Of_Same_Location()
        {
            IRepositoryOperationsCoordinator coordinator = CreateCoordinator();
            string destinationPath = CreateDestinationPath();
            await using IAsyncDisposable repairLease = await coordinator.AcquireRepairAsync(1, TestContext.Current.CancellationToken);

            Task<IAsyncDisposable> importAcquisition = coordinator.AcquireImportAsync(1, destinationPath, TestContext.Current.CancellationToken);

            Assert.False(importAcquisition.IsCompleted);

            await repairLease.DisposeAsync();
            await using IAsyncDisposable importLease = await importAcquisition;

            Assert.True(importAcquisition.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task Repair_Should_Wait_For_Import_Of_Same_Location()
        {
            IRepositoryOperationsCoordinator coordinator = CreateCoordinator();
            string destinationPath = CreateDestinationPath();
            await using IAsyncDisposable importLease = await coordinator.AcquireImportAsync(1, destinationPath, TestContext.Current.CancellationToken);

            Task<IAsyncDisposable> repairAcquisition = coordinator.AcquireRepairAsync(1, TestContext.Current.CancellationToken);

            Assert.False(repairAcquisition.IsCompleted);

            await importLease.DisposeAsync();
            await using IAsyncDisposable repairLease = await repairAcquisition;

            Assert.True(repairAcquisition.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task Repairs_Should_Be_Serialized_For_Same_Location()
        {
            IRepositoryOperationsCoordinator coordinator = CreateCoordinator();
            await using IAsyncDisposable firstLease = await coordinator.AcquireRepairAsync(1, TestContext.Current.CancellationToken);

            Task<IAsyncDisposable> secondAcquisition = coordinator.AcquireRepairAsync(1, TestContext.Current.CancellationToken);

            Assert.False(secondAcquisition.IsCompleted);

            await firstLease.DisposeAsync();
            await using IAsyncDisposable secondLease = await secondAcquisition;

            Assert.True(secondAcquisition.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task Operations_Should_Remain_Parallel_For_Different_Locations()
        {
            IRepositoryOperationsCoordinator coordinator = CreateCoordinator();
            await using IAsyncDisposable firstLocationRepair = await coordinator.AcquireRepairAsync(1, TestContext.Current.CancellationToken);

            Task<IAsyncDisposable> secondLocationImport = coordinator.AcquireImportAsync(
                2,
                CreateDestinationPath(),
                TestContext.Current.CancellationToken);

            await using IAsyncDisposable secondLocationImportLease = await secondLocationImport;

            Assert.True(secondLocationImport.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task Imports_Should_Remain_Parallel_For_Different_Destinations_In_Same_Location()
        {
            IRepositoryOperationsCoordinator coordinator = CreateCoordinator();
            await using IAsyncDisposable firstImport = await coordinator.AcquireImportAsync(
                1,
                CreateDestinationPath(),
                TestContext.Current.CancellationToken);

            Task<IAsyncDisposable> secondImportAcquisition = coordinator.AcquireImportAsync(
                1,
                CreateDestinationPath(),
                TestContext.Current.CancellationToken);

            await using IAsyncDisposable secondImport = await secondImportAcquisition;

            Assert.True(secondImportAcquisition.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task Imports_Should_Be_Serialized_For_Same_Canonical_Destination()
        {
            IRepositoryOperationsCoordinator coordinator = CreateCoordinator();
            string destinationPath = CreateDestinationPath();
            await using IAsyncDisposable firstImport = await coordinator.AcquireImportAsync(1, destinationPath, TestContext.Current.CancellationToken);

            Task<IAsyncDisposable> secondImportAcquisition = coordinator.AcquireImportAsync(
                1,
                destinationPath,
                TestContext.Current.CancellationToken);

            Assert.False(secondImportAcquisition.IsCompleted);

            await firstImport.DisposeAsync();
            await using IAsyncDisposable secondImport = await secondImportAcquisition;

            Assert.True(secondImportAcquisition.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task Cancellation_Should_Be_Propagated_While_Waiting_For_Repair()
        {
            IRepositoryOperationsCoordinator coordinator = CreateCoordinator();
            string destinationPath = CreateDestinationPath();
            await using IAsyncDisposable importLease = await coordinator.AcquireImportAsync(1, destinationPath, TestContext.Current.CancellationToken);
            using CancellationTokenSource cancellationTokenSource = new();

            Task<IAsyncDisposable> repairAcquisition = coordinator.AcquireRepairAsync(1, cancellationTokenSource.Token);

            Assert.False(repairAcquisition.IsCompleted);

            await cancellationTokenSource.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await repairAcquisition);
        }

        [Fact]
        public async Task Location_Should_Remain_Usable_After_Cancelled_Wait()
        {
            IRepositoryOperationsCoordinator coordinator = CreateCoordinator();
            string destinationPath = CreateDestinationPath();
            await using IAsyncDisposable importLease = await coordinator.AcquireImportAsync(1, destinationPath, TestContext.Current.CancellationToken);
            using CancellationTokenSource cancellationTokenSource = new();

            Task<IAsyncDisposable> cancelledRepairAcquisition = coordinator.AcquireRepairAsync(1, cancellationTokenSource.Token);

            Assert.False(cancelledRepairAcquisition.IsCompleted);

            await cancellationTokenSource.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await cancelledRepairAcquisition);

            await importLease.DisposeAsync();
            await using IAsyncDisposable repairLease = await coordinator.AcquireRepairAsync(1, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Location_Should_Be_Released_When_Operation_Throws()
        {
            IRepositoryOperationsCoordinator coordinator = CreateCoordinator();
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await using IAsyncDisposable lease = await coordinator.AcquireRepairAsync(1, TestContext.Current.CancellationToken);
                throw new InvalidOperationException("Controlled test failure.");
            });

            Assert.Equal("Controlled test failure.", exception.Message);

            await using IAsyncDisposable subsequentLease = await coordinator.AcquireRepairAsync(1, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Lease_Disposal_Should_Be_Idempotent()
        {
            IRepositoryOperationsCoordinator coordinator = CreateCoordinator();
            IAsyncDisposable lease = await coordinator.AcquireRepairAsync(1, TestContext.Current.CancellationToken);

            await lease.DisposeAsync();
            await lease.DisposeAsync();

            await using IAsyncDisposable subsequentLease = await coordinator.AcquireRepairAsync(1, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Opposite_Location_Request_Order_Should_Not_Deadlock_When_Locations_Are_Acquired_Individually()
        {
            IRepositoryOperationsCoordinator coordinator = CreateCoordinator();
            Task firstOperation = AcquireLocationsSequentiallyAsync(coordinator, 1, 2);
            Task secondOperation = AcquireLocationsSequentiallyAsync(coordinator, 2, 1);

            await Task.WhenAll(firstOperation, secondOperation);

            Assert.True(firstOperation.IsCompletedSuccessfully);
            Assert.True(secondOperation.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task Waiting_Repair_Should_Prevent_Later_Import_From_Overtaking()
        {
            IRepositoryOperationsCoordinator coordinator = CreateCoordinator();
            string firstDestinationPath = CreateDestinationPath();
            string secondDestinationPath = CreateDestinationPath();
            await using IAsyncDisposable firstImport = await coordinator.AcquireImportAsync(1, firstDestinationPath, TestContext.Current.CancellationToken);

            Task<IAsyncDisposable> repairAcquisition = coordinator.AcquireRepairAsync(1, TestContext.Current.CancellationToken);
            Task<IAsyncDisposable> laterImportAcquisition = coordinator.AcquireImportAsync(1, secondDestinationPath, TestContext.Current.CancellationToken);

            Assert.False(repairAcquisition.IsCompleted);
            Assert.False(laterImportAcquisition.IsCompleted);

            await firstImport.DisposeAsync();
            await using IAsyncDisposable repairLease = await repairAcquisition;

            Assert.True(repairAcquisition.IsCompletedSuccessfully);
            Assert.False(laterImportAcquisition.IsCompleted);

            await repairLease.DisposeAsync();
            await using IAsyncDisposable laterImportLease = await laterImportAcquisition;

            Assert.True(laterImportAcquisition.IsCompletedSuccessfully);
        }

        private static async Task AcquireLocationsSequentiallyAsync(IRepositoryOperationsCoordinator coordinator, int firstLocationId, int secondLocationId)
        {
            /*
             * Each location is released before the next is requested. This mirrors
             * the deliberate all-locations repair strategy and cannot create a
             * circular multi-location wait.
             */
            await using (IAsyncDisposable firstLease = await coordinator.AcquireRepairAsync(firstLocationId, TestContext.Current.CancellationToken))
            {
            }

            await using (IAsyncDisposable secondLease = await coordinator.AcquireRepairAsync(secondLocationId, TestContext.Current.CancellationToken))
            {
            }
        }

        private static IRepositoryOperationsCoordinator CreateCoordinator() => RepositoryOperationsCoordinator.Create(new TestRepositoryBase());

        private static string CreateDestinationPath() => Path.Combine(Path.GetTempPath(), "MoprRepositoryCoordinatorTests", Guid.NewGuid().ToString("N"), "Image.dcm");

        private sealed class TestRepositoryBase : IRepositoryBase
        {
            public MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.IApplicationConfiguration? ApplicationConfiguration => null;
            public Microsoft.Extensions.Logging.ILogger? Logger => null;
            public IRepositoryOperationsCoordinator? OperationsCoordinator => null;
            public MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts.IPersistence? Persistence => null;
            public void OnExceptionThrown(Exception exception) { }
        }
    }
}