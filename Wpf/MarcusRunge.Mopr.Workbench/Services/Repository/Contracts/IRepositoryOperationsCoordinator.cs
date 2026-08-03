namespace MarcusRunge.Mopr.Workbench.Services.Repository.Contracts
{
    /// <summary>
    /// Coordinates repository operations within the current MOPR process.
    /// </summary>
    internal interface IRepositoryOperationsCoordinator
    {
        /// <summary>
        /// Acquires shared access to one repository location followed by exclusive
        /// access to one canonical import destination path.
        /// </summary>
        /// <param name="repositoryLocationId">
        /// The persisted repository-location ID.
        /// </param>
        /// <param name="canonicalDestinationPath">
        /// The validated canonical absolute destination path.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used while waiting for access.
        /// </param>
        /// <returns>
        /// A lease that releases both acquisitions in reverse order.
        /// </returns>
        Task<IAsyncDisposable> AcquireImportAsync(int repositoryLocationId, string canonicalDestinationPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Acquires exclusive access to one repository location for verification
        /// or repair.
        /// </summary>
        /// <param name="repositoryLocationId">
        /// The persisted repository-location ID.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used while waiting for access.
        /// </param>
        /// <returns>
        /// A lease that releases the exclusive repository-location access.
        /// </returns>
        Task<IAsyncDisposable> AcquireRepairAsync(int repositoryLocationId, CancellationToken cancellationToken = default);
    }
}