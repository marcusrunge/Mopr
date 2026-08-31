namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Defines the public contract of the Persistence module.
    /// </summary>
    public interface IPersistence
    {
        /// <summary>
        /// Occurs when an exception is thrown by the Persistence module.
        /// </summary>
        event Action<Exception> ExceptionThrown;

        /// <summary>
        /// Gets the task representing the initialization of the most recently
        /// received Persistence configuration.
        /// </summary>
        /// <remarks>
        /// Consumers that publish a configuration must await this task before
        /// using Persistence-dependent application services.
        /// </remarks>
        Task Initialization { get; }

        /// <summary>
        /// Gets the atomic DICOM import Persistence service.
        /// </summary>
        IDicomImportPersistenceService? DicomImport { get; }

        /// <summary>
        /// Gets the instance repository.
        /// </summary>
        IInstanceRepository? Instance { get; }

        /// <summary>
        /// Gets the Persistence integrity service.
        /// </summary>
        IPersistenceIntegrityService? Integrity { get; }

        /// <summary>
        /// Gets the measurement repository.
        /// </summary>
        IMeasurementRepository? Measurement { get; }

        /// <summary>
        /// Gets the repository-location repository.
        /// </summary>
        IRepositoryLocationRepository? RepositoryLocation { get; }

        /// <summary>
        /// Gets the series repository.
        /// </summary>
        ISeriesRepository? Series { get; }

        /// <summary>
        /// Gets the study repository.
        /// </summary>
        IStudyRepository? Study { get; }

        /// <summary>
        /// Tests whether the supplied Persistence configuration can establish a
        /// database connection without replacing the active configuration.
        /// </summary>
        /// <param name="configuration">The Persistence configuration to test.</param>
        /// <param name="cancellationToken">Cancels the connection test.</param>
        /// <returns>The connection test result.</returns>
        Task<PersistenceConnectionTestResult> TestConnectionAsync(PersistenceConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the Unreal object repository.
        /// </summary>
        IUnrealObjectRepository? UnrealObject { get; }

        /// <summary>
        /// Gets the user repository.
        /// </summary>
        IUserRepository? User { get; }
    }
}