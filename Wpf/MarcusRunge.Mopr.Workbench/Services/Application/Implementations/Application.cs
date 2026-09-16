using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Bases;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations
{
    /// <summary>
    /// Composes the services owned by one application-service instance.
    /// </summary>
    internal sealed class Application : ApplicationBase
    {
        internal Application(ILogger? logger, IAuditIdentityProvider? auditIdentityProvider, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IPersistence? persistence, Repository.Contracts.IRepository? repository) : base(logger, auditIdentityProvider, operatingSystemIdentityProvider, persistence, repository)
        {
            _dialogService = Dialog.DialogService.Create(this);
            _mediaService = Media.MediaService.Create(this);

            // Identity is created before import because import attribution will
            // obtain its persistent user ID from the authenticated identity graph.
            _identityService = Identity.IdentityService.Create(this);
            _importService = Import.ImportService.Create(this);
        }
    }
}