using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity
{
    /// <summary>
    /// Defines the interface for an identity service.
    /// </summary>
    public interface IIdentityService
    {
        /// <summary>
        /// Gets the current-user context.
        /// </summary>
        ICurrentUserContext? CurrentUserContext { get; }
    }
}