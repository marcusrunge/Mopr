using MarcusRunge.Mopr.Workbench.Modules.Import.ViewModels;
using MarcusRunge.Mopr.Workbench.Modules.Import.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace MarcusRunge.Mopr.Workbench.Modules.Import
{
    /// <summary>
    /// Registers the DICOM import dialog.
    /// </summary>
    public sealed class ImportModule : IModule
    {
        /// <inheritdoc/>
        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        /// <inheritdoc/>
        public void RegisterTypes(IContainerRegistry containerRegistry) => containerRegistry.RegisterDialog<ImportView, ImportViewModel>();
    }
}