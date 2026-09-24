using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity;
using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Application.Startup
{
    /// <summary>
    /// Resolves the startup navigation target from the current MOPR user assignment.
    /// </summary>
    internal sealed class UserStartupRouteService(IApplication application) : IUserStartupRouteService
    {
        /// <inheritdoc/>
        public async Task<string> GetNavigationTargetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var signInService = application.IdentityService?.UserSignInService;

            if (signInService is null)
            {
                return NavigationNames.IdentityUnavailable;
            }

            var result = await signInService.SignInAsync(cancellationToken).ConfigureAwait(false);

            return result.Status switch
            {
                UserSignInStatus.SignedIn => NavigationNames.Imaging,
                UserSignInStatus.UserUnknown => NavigationNames.IdentityProvisioning,
                UserSignInStatus.UserDisabled => NavigationNames.IdentityBlocked,
                UserSignInStatus.NotStarted or
                UserSignInStatus.OperatingSystemIdentityUnavailable or
                UserSignInStatus.InvalidPersistentUserId or
                UserSignInStatus.PersistenceUnavailable or
                UserSignInStatus.Failed => NavigationNames.IdentityUnavailable,
                _ => NavigationNames.IdentityUnavailable
            };
        }
    }
}