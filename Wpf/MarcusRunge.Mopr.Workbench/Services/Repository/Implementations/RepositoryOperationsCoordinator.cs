using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Implementations
{
    /*
     * Central in-process coordinator for repository operations.
     *
     * The mandatory lock hierarchy is:
     *
     * Repository-location access
     * └── canonical import destination access
     *
     * Imports share location access so imports targeting different files remain
     * parallel. Repairs receive exclusive location access.
     */
    internal class RepositoryOperationsCoordinator : CreateableBindableBase<IRepositoryOperationsCoordinator, RepositoryOperationsCoordinator, IRepositoryBase>, IRepositoryOperationsCoordinator
    {
        /*
         * Dictionary membership and reference counts are protected by this monitor.
         * Every holder and waiter reserves one reference before an entry escapes
         * the monitor.
         */
        private readonly Dictionary<string, DestinationLockEntry> _destinationLocks = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, LocationLockEntry> _locationLocks = [];
        private readonly object _lockEntriesSyncRoot = new();

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> AcquireImportAsync(int repositoryLocationId, string canonicalDestinationPath, CancellationToken cancellationToken = default)
        {
            ValidateRepositoryLocationId(repositoryLocationId);
            ArgumentException.ThrowIfNullOrWhiteSpace(canonicalDestinationPath);

            if (!Path.IsPathFullyQualified(canonicalDestinationPath))
            {
                throw new ArgumentException("The canonical repository destination path must be fully qualified.", nameof(canonicalDestinationPath));
            }

            string destinationLockKey = Path.GetFullPath(canonicalDestinationPath);
            LocationLockEntry locationEntry = ReserveLocationEntry(repositoryLocationId);
            bool locationAccessAcquired = false;

            try
            {
                /*
                 * Shared location access is always acquired before destination access.
                 * No coordinator path acquires these locks in the opposite order.
                 */
                await AcquireLocationReaderAsync(locationEntry, cancellationToken);
                locationAccessAcquired = true;

                DestinationLockEntry destinationEntry = ReserveDestinationEntry(destinationLockKey);
                bool destinationAccessAcquired = false;

                try
                {
                    await destinationEntry.Semaphore.WaitAsync(cancellationToken);
                    destinationAccessAcquired = true;

                    return new ImportLease(
                        this,
                        repositoryLocationId,
                        locationEntry,
                        destinationLockKey,
                        destinationEntry);
                }
                catch
                {
                    /*
                     * A cancelled wait owns its dictionary reservation but does not
                     * own the semaphore permit unless WaitAsync completed.
                     */
                    if (destinationAccessAcquired)
                    {
                        destinationEntry.Semaphore.Release();
                    }

                    ReleaseDestinationEntry(destinationLockKey, destinationEntry);
                    throw;
                }
            }
            catch
            {
                /*
                 * OperationCanceledException is deliberately not translated. Shared
                 * location state is released only if acquisition completed.
                 */
                if (locationAccessAcquired)
                {
                    await ReleaseLocationReaderAsync(repositoryLocationId, locationEntry);
                }
                else
                {
                    ReleaseLocationEntry(repositoryLocationId, locationEntry);
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IAsyncDisposable> AcquireRepairAsync(int repositoryLocationId, CancellationToken cancellationToken = default)
        {
            ValidateRepositoryLocationId(repositoryLocationId);

            LocationLockEntry locationEntry = ReserveLocationEntry(repositoryLocationId);
            bool turnstileAcquired = false;
            bool exclusiveLocationAccessAcquired = false;

            try
            {
                /*
                 * A repair retains the turnstile while waiting for active imports to
                 * leave. New imports therefore cannot continually overtake a waiting
                 * repair.
                 */
                await locationEntry.ReaderTurnstile.WaitAsync(cancellationToken);
                turnstileAcquired = true;

                await locationEntry.LocationEmpty.WaitAsync(cancellationToken);
                exclusiveLocationAccessAcquired = true;

                return new RepairLease(this, repositoryLocationId, locationEntry);
            }
            catch
            {
                /*
                 * Only successfully acquired semaphore permits are released. A
                 * cancelled wait cannot leak or manufacture a permit.
                 */
                if (exclusiveLocationAccessAcquired)
                {
                    locationEntry.LocationEmpty.Release();
                }

                if (turnstileAcquired)
                {
                    locationEntry.ReaderTurnstile.Release();
                }

                ReleaseLocationEntry(repositoryLocationId, locationEntry);
                throw;
            }
        }

        protected override void OnCreate(IRepositoryBase @base) => ArgumentNullException.ThrowIfNull(@base);

        protected override Task OnCreateAsync(IRepositoryBase @base, CancellationToken cancellationToken) => Task.CompletedTask;

        private static async Task AcquireLocationReaderAsync(LocationLockEntry entry, CancellationToken cancellationToken)
        {
            bool turnstileAcquired = false;
            bool readerMutexAcquired = false;

            try
            {
                /*
                 * Imports pass through the turnstile but do not retain it. A waiting
                 * repair retains the turnstile and prevents later imports from
                 * entering the reader group.
                 */
                await entry.ReaderTurnstile.WaitAsync(cancellationToken);
                turnstileAcquired = true;

                await entry.ReaderMutex.WaitAsync(cancellationToken);
                readerMutexAcquired = true;

                if (entry.ActiveReaders == 0)
                {
                    /*
                     * The first import reserves the location for the entire reader
                     * group. The reader count changes only after this acquisition
                     * succeeds.
                     */
                    await entry.LocationEmpty.WaitAsync(cancellationToken);
                }

                entry.ActiveReaders++;
            }
            finally
            {
                if (readerMutexAcquired)
                {
                    entry.ReaderMutex.Release();
                }

                if (turnstileAcquired)
                {
                    entry.ReaderTurnstile.Release();
                }
            }
        }

        private DestinationLockEntry ReserveDestinationEntry(string destinationLockKey)
        {
            lock (_lockEntriesSyncRoot)
            {
                if (!_destinationLocks.TryGetValue(destinationLockKey, out DestinationLockEntry? entry))
                {
                    entry = new DestinationLockEntry();
                    _destinationLocks.Add(destinationLockKey, entry);
                }

                /*
                 * The reservation exists before the entry escapes the monitor.
                 * Removal is impossible while this caller holds or awaits it.
                 */
                entry.ReferenceCount++;
                return entry;
            }
        }

        private LocationLockEntry ReserveLocationEntry(int repositoryLocationId)
        {
            lock (_lockEntriesSyncRoot)
            {
                if (!_locationLocks.TryGetValue(repositoryLocationId, out LocationLockEntry? entry))
                {
                    entry = new LocationLockEntry();
                    _locationLocks.Add(repositoryLocationId, entry);
                }

                entry.ReferenceCount++;
                return entry;
            }
        }

        private void ReleaseDestinationEntry(string destinationLockKey, DestinationLockEntry entry)
        {
            lock (_lockEntriesSyncRoot)
            {
                if (entry.ReferenceCount <= 0)
                {
                    throw new InvalidOperationException($"Destination lock '{destinationLockKey}' has no reference to release.");
                }

                entry.ReferenceCount--;

                /*
                 * Zero references prove that no coordinator caller can still hold or
                 * await the entry. Reference equality prevents an obsolete release
                 * from removing a newer entry registered for the same path.
                 */
                if (entry.ReferenceCount == 0
                    && _destinationLocks.TryGetValue(destinationLockKey, out DestinationLockEntry? currentEntry)
                    && ReferenceEquals(currentEntry, entry))
                {
                    _destinationLocks.Remove(destinationLockKey);
                }
            }
        }

        private void ReleaseLocationEntry(int repositoryLocationId, LocationLockEntry entry)
        {
            lock (_lockEntriesSyncRoot)
            {
                if (entry.ReferenceCount <= 0)
                {
                    throw new InvalidOperationException($"Repository-location lock '{repositoryLocationId}' has no reference to release.");
                }

                entry.ReferenceCount--;

                /*
                 * Entries are retired only after the final holder or waiter releases
                 * its reservation. SemaphoreSlim instances are not disposed here,
                 * avoiding a disposal race with code that already references them.
                 */
                if (entry.ReferenceCount == 0
                    && _locationLocks.TryGetValue(repositoryLocationId, out LocationLockEntry? currentEntry)
                    && ReferenceEquals(currentEntry, entry))
                {
                    _locationLocks.Remove(repositoryLocationId);
                }
            }
        }

        private async ValueTask ReleaseImportAsync(int repositoryLocationId, LocationLockEntry locationEntry, string destinationLockKey, DestinationLockEntry destinationEntry)
        {
            /*
             * The destination lock is released before the import leaves its shared
             * location access. This is the reverse acquisition order.
             */
            destinationEntry.Semaphore.Release();
            ReleaseDestinationEntry(destinationLockKey, destinationEntry);

            await ReleaseLocationReaderAsync(repositoryLocationId, locationEntry);
        }

        private async ValueTask ReleaseLocationReaderAsync(int repositoryLocationId, LocationLockEntry entry)
        {
            /*
             * Release does not use the operation's cancellation token. Once an import
             * owns a lease, cleanup must complete after success, failure,
             * cancellation or failed compensation.
             */
            await entry.ReaderMutex.WaitAsync();

            try
            {
                if (entry.ActiveReaders <= 0)
                {
                    throw new InvalidOperationException($"Repository-location lock '{repositoryLocationId}' has no active import reader to release.");
                }

                entry.ActiveReaders--;

                if (entry.ActiveReaders == 0)
                {
                    entry.LocationEmpty.Release();
                }
            }
            finally
            {
                entry.ReaderMutex.Release();
                ReleaseLocationEntry(repositoryLocationId, entry);
            }
        }

        private void ReleaseRepair(int repositoryLocationId, LocationLockEntry entry)
        {
            /*
             * Exclusive access was acquired after the turnstile. Reverse release
             * makes the location available before new imports pass the turnstile.
             */
            entry.LocationEmpty.Release();
            entry.ReaderTurnstile.Release();
            ReleaseLocationEntry(repositoryLocationId, entry);
        }

        private static void ValidateRepositoryLocationId(int repositoryLocationId)
        {
            if (repositoryLocationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(repositoryLocationId), "The repository-location ID must be a positive integer.");
            }
        }

        private sealed class DestinationLockEntry
        {
            internal int ReferenceCount { get; set; }
            internal SemaphoreSlim Semaphore { get; } = new(1, 1);
        }

        private sealed class ImportLease(
            RepositoryOperationsCoordinator owner,
            int repositoryLocationId,
            LocationLockEntry locationEntry,
            string destinationLockKey,
            DestinationLockEntry destinationEntry) : IAsyncDisposable
        {
            private RepositoryOperationsCoordinator? _owner = owner;

            public ValueTask DisposeAsync()
            {
                /*
                 * Idempotent disposal prevents a duplicate semaphore release if a
                 * defensive cleanup path disposes the lease more than once.
                 */
                RepositoryOperationsCoordinator? currentOwner = Interlocked.Exchange(ref _owner, null);

                return currentOwner is null
                    ? ValueTask.CompletedTask
                    : currentOwner.ReleaseImportAsync(repositoryLocationId, locationEntry, destinationLockKey, destinationEntry);
            }
        }

        private sealed class LocationLockEntry
        {
            internal int ActiveReaders { get; set; }
            internal SemaphoreSlim LocationEmpty { get; } = new(1, 1);
            internal SemaphoreSlim ReaderMutex { get; } = new(1, 1);
            internal SemaphoreSlim ReaderTurnstile { get; } = new(1, 1);
            internal int ReferenceCount { get; set; }
        }

        private sealed class RepairLease(RepositoryOperationsCoordinator owner, int repositoryLocationId, LocationLockEntry locationEntry) : IAsyncDisposable
        {
            private RepositoryOperationsCoordinator? _owner = owner;

            public ValueTask DisposeAsync()
            {
                RepositoryOperationsCoordinator? currentOwner = Interlocked.Exchange(ref _owner, null);

                if (currentOwner is null)
                {
                    return ValueTask.CompletedTask;
                }

                currentOwner.ReleaseRepair(repositoryLocationId, locationEntry);
                return ValueTask.CompletedTask;
            }
        }
    }
}