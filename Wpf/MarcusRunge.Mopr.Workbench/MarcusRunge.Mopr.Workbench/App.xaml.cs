using MarcusRunge.Mopr.Workbench.Modules.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging;
using MarcusRunge.Mopr.Workbench.Views;
using Prism.Ioc;
using Prism.Modularity;
using System.Windows;

namespace MarcusRunge.Mopr.Workbench
{
    public partial class App
    {
        protected override Window CreateShell() => Container.Resolve<MainWindow>();

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IImagingSelectionService, ImagingSelectionService>();
            containerRegistry.RegisterSingleton<IImagingToolService, ImagingToolService>();
            containerRegistry.RegisterSingleton<IImagingViewportService, ImagingViewportService>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog) => moduleCatalog.AddModule<ImagingModule>();
    }
}