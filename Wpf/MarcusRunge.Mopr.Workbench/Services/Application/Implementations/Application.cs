using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Bases;
using MarcusRunge.Mopr.Workbench.Services.Application.Implementations.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Implementations.RepositoryRecovery;
using MarcusRunge.Mopr.Workbench.Services.Application.Implementations.UserAdministration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations;

/// <summary>
/// Composes the services owned by one application-service instance.
/// </summary>
internal sealed class Application : ApplicationBase
{
    internal Application(ILogger? logger, IAdministrativeAuthorizationService? administrativeAuthorizationService, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IPersistence? persistence, Repository.Contracts.IRepository? repository, IMachineConfigurationService? machineConfigurationService) : base(logger, administrativeAuthorizationService, operatingSystemIdentityProvider, persistence, repository, machineConfigurationService)
    {
        _dialogService = Dialog.DialogService.Create(this);
        _mediaService = Media.MediaService.Create(this);

        // Identity must be created before user-facing administration services because they use the current persistent user as their audit or selection context.
        _identityService = Identity.IdentityService.Create(this);
        _repositoryAdministration = RepositoryLocationAdministrationService.Create(this, CreationLifetime.Scoped);
        _repositoryRecovery = RepositoryRecoveryActionService.Create(this, CreationLifetime.Scoped);
        _userAdministration = UserAdministrationService.Create(this, CreationLifetime.Scoped);
        _importService = Import.ImportService.Create(this);
    }
}