using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup
{
    /// <summary>
    /// Registers the machine-wide MOPR setup user interface.
    /// </summary>
    public sealed class SetupModule : IModule
    {
        /// <inheritdoc/>
        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        /// <inheritdoc/>
        public void RegisterTypes(IContainerRegistry containerRegistry) => containerRegistry.RegisterForNavigation<SetupView>(NavigationNames.Setup);
    }
}