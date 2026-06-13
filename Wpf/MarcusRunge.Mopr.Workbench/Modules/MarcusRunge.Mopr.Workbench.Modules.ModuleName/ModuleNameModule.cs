using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Modules.ModuleName.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;

namespace MarcusRunge.Mopr.Workbench.Modules.ModuleName
{
    public class ModuleNameModule(IRegionManager regionManager) : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider) => regionManager.RequestNavigate(RegionNames.ContentRegion, "ViewA");

        public void RegisterTypes(IContainerRegistry containerRegistry) => containerRegistry.RegisterForNavigation<ViewA>();
    }
}