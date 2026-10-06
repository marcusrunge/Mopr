using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Modules.Miras.Views;

namespace MarcusRunge.Mopr.Workbench.Modules.Miras
{
    /// <summary>
    /// Registers the MOPR integrity and recovery user interface.
    /// </summary>
    public sealed class MirasModule : IModule
    {
        /// <inheritdoc/>
        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        /// <inheritdoc/>
        public void RegisterTypes(IContainerRegistry containerRegistry) => containerRegistry.RegisterForNavigation<MirasActionRequiredView>(NavigationNames.MirasActionRequired);
    }
}