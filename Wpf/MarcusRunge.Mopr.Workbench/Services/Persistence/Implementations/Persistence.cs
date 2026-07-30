using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Bases;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Implementations
{
    /// <summary>
    /// Provides the concrete Persistence module and composes its repositories
    /// and services.
    /// </summary>
    internal class Persistence : PersistenceBase
    {
        internal Persistence(ILogger? logger, IApplicationLifetime? applicationLifetime, IObservable<PersistenceConfiguration> persistenceConfigurationObservable) : base(logger, applicationLifetime, persistenceConfigurationObservable)
        {
            /*
             * Granular repositories remain available for normal entity operations.
             * The DICOM import service deliberately receives the Persistence base
             * directly so one import can use exactly one DbContext instead of
             * composing multiple independently saving repositories.
             */
            _instance = InstanceRepository.Create(this);
            _measurement = MeasurementRepository.Create(this);
            _repositoryLocation = RepositoryLocationRepository.Create(this);
            _series = SeriesRepository.Create(this);
            _study = StudyRepository.Create(this);
            _unrealObject = UnrealObjectRepository.Create(this);
            _user = UserRepository.Create(this);
            _dicomImport = DicomImportPersistenceService.Create(this);

            /*
             * Integrity verification consumes the fully composed public read
             * contracts and is therefore created after all repositories.
             */
            _integrity = PersistenceIntegrityService.Create(this);
        }
    }
}