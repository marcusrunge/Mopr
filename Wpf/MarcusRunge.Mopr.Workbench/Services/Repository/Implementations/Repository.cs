using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Bases;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Implementations
{
    // Concrete internal module implementation that wires up services for this module instance.
    internal class Repository : RepositoryBase
    {
        internal Repository(ILogger? logger, IApplicationLifetime? applicationLifetime, IObservable<IApplicationConfiguration>? applicationConfigurationObservable, IPersistence? persistence) : base(logger, applicationLifetime, applicationConfigurationObservable, persistence)
        {
            /*
             * The coordinator is created first so import and repair resolve the same
             * module-owned instance through IRepositoryBase.
             *
             * Repository path validation is initialized before the operations that
             * depend on canonical repository paths.
             */
            _operationsCoordinator = RepositoryOperationsCoordinator.Create(this);
            _repositoryService = DicomRepositoryService.Create(this);
            _importService = DicomImportService.Create(this);
            _repositoryRepairService = DicomRepositoryRepairService.Create(this);
        }
    }
}