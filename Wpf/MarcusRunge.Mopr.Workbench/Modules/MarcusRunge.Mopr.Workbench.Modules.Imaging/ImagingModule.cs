using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging
{
    public class ImagingModule(IRegionManager regionManager) : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            //regionManager.RequestNavigate(RegionNames.ContentRegion, "ImagingWorkbenchView");
            regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(ImagingWorkbenchView));
            regionManager.RegisterViewWithRegion(RegionNames.ImagingCommandBarRegion, typeof(ImagingCommandBarView));
            regionManager.RegisterViewWithRegion(RegionNames.PropertiesRegion, typeof(PropertiesPanelView));
            regionManager.RegisterViewWithRegion(RegionNames.SeriesRegion, typeof(SeriesPanelView));
            regionManager.RegisterViewWithRegion(RegionNames.ViewerRegion, typeof(ImageViewerView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<ImageViewerView>();
            containerRegistry.RegisterForNavigation<ImagingCommandBarView>();
            containerRegistry.RegisterForNavigation<ImagingWorkbenchView>();
            containerRegistry.RegisterForNavigation<PropertiesPanelView>();
            containerRegistry.RegisterForNavigation<SeriesPanelView>();
        }
    }
}