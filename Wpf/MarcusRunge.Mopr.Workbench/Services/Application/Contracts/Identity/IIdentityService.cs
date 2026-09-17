using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity
{
    /// <summary>
    /// Defines the public identity services owned by one application-service instance.
    /// </summary>
    public interface IIdentityService
    {
        /// <summary>
        /// Gets the current-user context.
        /// </summary>
        ICurrentUserContext? CurrentUserContext { get; }

        /// <summary>
        /// Gets the service that creates a persistent MOPR user for the current operating-system identity.
        /// </summary>
        IUserProvisioningService? UserProvisioningService { get; }

        /// <summary>
        /// Gets the service that resolves and signs in the current operating-system user.
        /// </summary>
        IUserSignInService? UserSignInService { get; }
    }
}