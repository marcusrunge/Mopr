using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Bases;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Identity
{
    /// <summary>
    /// Provides the identity services owned by one application-service instance.
    /// </summary>
    internal sealed class IdentityService : IdentityServiceBase
    {
        private IdentityService(IApplicationBase applicationBase) : base(applicationBase)
        {
            // The context manager must exist before all services that read or
            // publish the signed-in persistent MOPR user.
            _currentUserContext = CurrentUserContextManager.Create(this, CreationLifetime.Scoped);
            _auditIdentityProvider = CurrentUserAuditIdentityProvider.Create(this, CreationLifetime.Scoped);
            _userSignInService = Identity.UserSignInService.Create(this, CreationLifetime.Scoped);
            _userProvisioningService = Identity.UserProvisioningService.Create(this, CreationLifetime.Scoped);
        }

        internal static IIdentityService? Create(IApplicationBase? applicationBase) => applicationBase is null ? null : new IdentityService(applicationBase);
    }
}