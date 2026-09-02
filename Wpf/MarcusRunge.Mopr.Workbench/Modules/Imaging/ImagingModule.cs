using MarcusRunge.Mopr.Workbench.Core;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging
{
    /// <summary>
    /// Registers the MOPR imaging workbench and its contained views.
    /// </summary>
    public sealed class ImagingModule(IRegionManager regionManager) : IModule
    {
        private readonly IRegionManager _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

        /// <inheritdoc/>
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // Child views remain registered with their dedicated regions. These
            // regions are created when the navigated ImagingWorkbenchView loads.
            _regionManager.RegisterViewWithRegion(RegionNames.ImagingCommandBarRegion, typeof(ImagingCommandBarView));
            _regionManager.RegisterViewWithRegion(RegionNames.PropertiesRegion, typeof(PropertiesPanelView));
            _regionManager.RegisterViewWithRegion(RegionNames.SeriesRegion, typeof(SeriesPanelView));
            _regionManager.RegisterViewWithRegion(RegionNames.ViewerRegion, typeof(ImageViewerView));
        }

        /// <inheritdoc/>
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IImagingMeasurementContext, ImagingMeasurementContext>();
            containerRegistry.RegisterForNavigation<ImageViewerView>();
            containerRegistry.RegisterForNavigation<ImagingCommandBarView>();
            containerRegistry.RegisterForNavigation<ImagingWorkbenchView>(NavigationNames.Imaging);
            containerRegistry.RegisterForNavigation<PropertiesPanelView>();
            containerRegistry.RegisterForNavigation<SeriesPanelView>();
        }
    }
}