using MarcusRunge.Mopr.Workbench.Contracts.Application;
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
             * Repositories are created before the integrity service because
             * integrity verification consumes their public read contracts.
             *
             * No repository performs integrity verification during creation,
             * so the composition remains deterministic and free of cyclic
             * initialization dependencies.
             */
            _instance = InstanceRepository.Create(this);
            _measurement = MeasurementRepository.Create(this);
            _repositoryLocation = RepositoryLocationRepository.Create(this);
            _series = SeriesRepository.Create(this);
            _study = StudyRepository.Create(this);
            _unrealObject = UnrealObjectRepository.Create(this);
            _user = UserRepository.Create(this);
            _integrity = PersistenceIntegrityService.Create(this);
        }
    }
}