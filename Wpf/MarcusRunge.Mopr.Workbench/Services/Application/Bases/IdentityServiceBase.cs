using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Bases
{
    /// <summary>
    /// Provides the shared state and application dependencies of an identity-service instance.
    /// </summary>
    internal abstract class IdentityServiceBase(IApplicationBase? applicationBase) : IIdentityServiceBase, IIdentityService
    {
        protected IAuditIdentityProvider? _auditIdentityProvider;
        protected ICurrentUserContextManager? _currentUserContext;
        protected IUserProvisioningService? _userProvisioningService;
        protected IUserSignInService? _userSignInService;

        /// <inheritdoc/>
        IApplicationBase? IServiceBase.ApplicationBase => applicationBase;

        /// <inheritdoc/>
        IAuditIdentityProvider? IIdentityServiceBase.AuditIdentityProvider => _auditIdentityProvider;

        /// <inheritdoc/>
        ICurrentUserContextManager? IIdentityServiceBase.CurrentUserContextManager => _currentUserContext;

        /// <inheritdoc/>
        public ICurrentUserContext? CurrentUserContext => _currentUserContext;

        /// <inheritdoc/>
        public IUserProvisioningService? UserProvisioningService => _userProvisioningService;

        /// <inheritdoc/>
        public IUserSignInService? UserSignInService => _userSignInService;
    }
}