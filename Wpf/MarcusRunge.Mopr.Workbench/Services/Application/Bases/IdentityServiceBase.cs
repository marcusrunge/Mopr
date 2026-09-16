using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Bases
{
    /// <summary>
    /// Provides the shared state and application dependencies of an identity-service instance.
    /// </summary>
    internal abstract class IdentityServiceBase(IApplicationBase? applicationBase) : IIdentityServiceBase, IIdentityService
    {
        protected ICurrentUserContextManager? _currentUserContext;

        /// <inheritdoc/>
        IApplicationBase? IServiceBase.ApplicationBase => applicationBase;

        /// <inheritdoc/>
        ICurrentUserContextManager? IIdentityServiceBase.CurrentUserContextManager => _currentUserContext;

        /// <inheritdoc/>
        public ICurrentUserContext? CurrentUserContext => _currentUserContext;
    }
}