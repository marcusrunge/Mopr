using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Modules.Identity.Views;

namespace MarcusRunge.Mopr.Workbench.Modules.Identity
{
    /// <summary>
    /// Registers the MOPR user identity user interface.
    /// </summary>
    public sealed class IdentityModule : IModule
    {
        /// <inheritdoc/>
        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        /// <inheritdoc/>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<UserProvisioningView>(NavigationNames.IdentityProvisioning);
            containerRegistry.RegisterForNavigation<UserBlockedView>(NavigationNames.IdentityBlocked);
            containerRegistry.RegisterForNavigation<IdentityUnavailableView>(NavigationNames.IdentityUnavailable);
        }
    }
}