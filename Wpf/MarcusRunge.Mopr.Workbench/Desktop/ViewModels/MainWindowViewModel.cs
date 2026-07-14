using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Properties;
using Prism.Mvvm;

namespace MarcusRunge.Mopr.Workbench.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string _title = Resources.MainWindowTitle;
        public string Title { get => _title; set => SetProperty(ref _title, value); }
    }
}