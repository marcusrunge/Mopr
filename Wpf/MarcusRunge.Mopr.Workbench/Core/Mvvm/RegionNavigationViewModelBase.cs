using Prism.Navigation.Regions;

namespace MarcusRunge.Mopr.Workbench.Core.Mvvm
{
    public abstract class RegionNavigationViewModelBase(IRegionManager regionManager) : ViewModelBase, INavigationAware
    {
        protected IRegionManager RegionManager { get; } = regionManager;

        public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }
    }
}