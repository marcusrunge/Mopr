using Prism.Mvvm;

namespace MarcusRunge.Mopr.Workbench.Modules.Identity.ViewModels
{
    public class UserProvisioningViewModel : BindableBase
    {
        private string _message;

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public UserProvisioningViewModel() => Message = "View A from your Prism Module";
    }
}