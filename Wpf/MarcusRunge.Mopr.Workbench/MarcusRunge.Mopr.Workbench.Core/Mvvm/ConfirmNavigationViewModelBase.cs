using Prism.Navigation.Regions;
using System;

namespace MarcusRunge.Mopr.Workbench.Core.Mvvm
{
    public abstract class ConfirmNavigationViewModelBase(IRegionManager regionManager) : RegionNavigationViewModelBase(regionManager), IConfirmNavigationRequest
    {
        public virtual void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
        {
            continuationCallback(true);
        }
    }
}