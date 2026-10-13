using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Bases;
using MarcusRunge.Mopr.Workbench.Services.Application.Implementations.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations;

/// <summary>
/// Composes the services owned by one application-service instance.
/// </summary>
internal sealed class Application : ApplicationBase
{
    internal Application(ILogger? logger, IAdministrativeAuthorizationService? administrativeAuthorizationService, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IPersistence? persistence, Repository.Contracts.IRepository? repository)
        : base(logger, administrativeAuthorizationService, operatingSystemIdentityProvider, persistence, repository)
    {
        _dialogService = Dialog.DialogService.Create(this);
        _mediaService = Media.MediaService.Create(this);

        // Identity must be created before repository administration because
        // administrative repository changes require the current audit identity.
        _identityService = Identity.IdentityService.Create(this);

        _repositoryAdministration = RepositoryLocationAdministrationService.Create(this, CreationLifetime.Scoped);
        _importService = Import.ImportService.Create(this);
    }
}