using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity
{
    /// <summary>
    /// Provides the internal base contract for identity services.
    /// </summary>
    internal interface IIdentityServiceBase : IServiceBase
    {
        /// <summary>
        /// Gets the audit identity provider backed by the current-user context.
        /// </summary>
        internal IAuditIdentityProvider? AuditIdentityProvider { get; }

        /// <summary>
        /// Gets the current-user context manager owned by the identity-service graph.
        /// </summary>
        internal ICurrentUserContextManager? CurrentUserContextManager { get; }
    }
}