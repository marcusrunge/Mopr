using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Miras.Bases;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Implementations
{
    /// <summary>
    /// Composes the services owned by one MIRAS module instance.
    /// </summary>
    internal sealed class Miras : MirasBase
    {
        internal Miras(ILogger? logger, ILifetimeService? applicationLifetime, IPersistence persistence, IRepository repository, IAdministrativeAuthorizationService administrativeAuthorizationService, IRepositoryLocationValidationService repositoryLocationValidationService, ISystemAuditIdentityProvider systemAuditIdentityProvider) : base(logger, applicationLifetime, persistence, repository, administrativeAuthorizationService, repositoryLocationValidationService, systemAuditIdentityProvider)
        {
            // The MIRAS root remains the stable scope identity for operations,
            // recovery and the higher-level integrity flow.
            _operations = Implementations.Operations.Create(this, CreationLifetime.Scoped);
            _repositoryInfrastructureRecovery = RepositoryInfrastructureRecoveryService.Create(this, CreationLifetime.Scoped);
            _flow = Implementations.Flow.Create(this, CreationLifetime.Scoped);
        }
    }
}