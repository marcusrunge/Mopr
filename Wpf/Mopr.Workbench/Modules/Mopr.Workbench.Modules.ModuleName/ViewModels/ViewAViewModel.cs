using Mopr.Workbench.Core.Mvvm;
using Mopr.Workbench.Services.Interfaces;
using Prism.Navigation.Regions;

namespace Mopr.Workbench.Modules.ModuleName.ViewModels
{
    public class ViewAViewModel : RegionViewModelBase
    {
        private string _message;
        public string Message { get => _message; set => SetProperty(ref _message, value); }

        public ViewAViewModel(IRegionManager regionManager, IMessageService messageService) :
            base(regionManager)
        {
            Message = messageService.GetMessage();
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
        }
    }
}