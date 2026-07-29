namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Defines the public contract of the assembly.
    /// </summary>
    public interface IPersistence
    {
        /// <summary>
        /// Occurs when an exception is thrown.
        /// </summary>
        event Action<Exception> ExceptionThrown;

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
        /// Gets the Unreal object repository.
        /// </summary>
        IUnrealObjectRepository? UnrealObject { get; }

        /// <summary>
        /// Gets the user repository.
        /// </summary>
        IUserRepository? User { get; }
    }
}