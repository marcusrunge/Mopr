using MarcusRunge.Mopr.Workbench.Properties;
using Prism.Mvvm;

namespace MarcusRunge.Mopr.Workbench.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = Resources.MainWindowTitle;
        public string Title { get => _title; set => SetProperty(ref _title, value); }
    }
}