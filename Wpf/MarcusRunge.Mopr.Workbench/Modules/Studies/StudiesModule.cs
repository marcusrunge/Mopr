using MarcusRunge.Mopr.Workbench.Modules.Studies.ViewModels;
using MarcusRunge.Mopr.Workbench.Modules.Studies.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace MarcusRunge.Mopr.Workbench.Modules.Studies
{
    public class StudiesModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
        }

        public void RegisterTypes(IContainerRegistry containerRegistry) => containerRegistry.RegisterDialog<StudySelectionView, StudySelectionViewModel>();
    }
}