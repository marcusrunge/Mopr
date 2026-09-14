using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Bases;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Implementations
{
    /// <summary>
    /// Composes the services owned by one repository module instance.
    /// </summary>
    internal sealed class Repository : RepositoryBase
    {
        internal Repository(ILogger? logger, ILifetimeService? applicationLifetime, IObservable<IApplicationConfiguration>? applicationConfigurationObservable, IPersistence? persistence) : base(logger, applicationLifetime, applicationConfigurationObservable, persistence)
        {
            // The repository instance is the stable owner and dependency context for all
            // contained services. Scoped creation prevents independent repository graphs
            // from sharing services or retaining dependencies from an earlier factory.
            _operationsCoordinator = RepositoryOperationsCoordinator.Create(this, CreationLifetime.Scoped);
            _repositoryService = DicomRepositoryService.Create(this, CreationLifetime.Scoped);
            _importService = DicomImportService.Create(this, CreationLifetime.Scoped);
            _repositoryRepairService = DicomRepositoryRepairService.Create(this, CreationLifetime.Scoped);
        }
    }
}