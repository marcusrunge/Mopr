using Mopr.Workbench.Modules.ModuleName;
using Mopr.Workbench.Services;
using Mopr.Workbench.Services.Interfaces;
using Mopr.Workbench.Views;
using Prism.Ioc;
using Prism.Modularity;
using System.Windows;

namespace Mopr.Workbench
{
    public partial class App
    {
        protected override Window CreateShell() => Container.Resolve<MainWindow>();

        protected override void RegisterTypes(IContainerRegistry containerRegistry) => containerRegistry.RegisterSingleton<IMessageService, MessageService>();

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog) => moduleCatalog.AddModule<ModuleNameModule>();
    }
}