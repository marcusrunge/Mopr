using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using Prism.Commands;
using Prism.Mvvm;
using System.Windows;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public class ImagingWorkbenchViewModel : ViewModelBase
    {
        private const double DefaultSeriesPaneWidth = 280;
        private const double DefaultPropertiesPaneWidth = 340;

        private bool _isSeriesPaneVisible = true;
        private bool _isPropertiesPaneVisible = true;

        public ImagingWorkbenchViewModel()
        {
            ToggleSeriesPaneCommand = new DelegateCommand(ToggleSeriesPane);
            TogglePropertiesPaneCommand = new DelegateCommand(TogglePropertiesPane);
        }

        public string CurrentStudyDisplayText => "Studie: Keine Studie geöffnet";

        public string CurrentSeriesDisplayText => "Serie: Keine Serie aktiv";

        public DelegateCommand ToggleSeriesPaneCommand { get; }

        public DelegateCommand TogglePropertiesPaneCommand { get; }

        public bool IsSeriesPaneVisible
        {
            get => _isSeriesPaneVisible;
            private set
            {
                if (SetProperty(ref _isSeriesPaneVisible, value))
                {
                    RaisePropertyChanged(nameof(IsSeriesPaneCollapsed));
                    RaisePropertyChanged(nameof(SeriesPaneWidth));
                    RaisePropertyChanged(nameof(LeftSplitterWidth));
                }
            }
        }

        public bool IsSeriesPaneCollapsed => !IsSeriesPaneVisible;

        public bool IsPropertiesPaneVisible
        {
            get => _isPropertiesPaneVisible;
            private set
            {
                if (SetProperty(ref _isPropertiesPaneVisible, value))
                {
                    RaisePropertyChanged(nameof(IsPropertiesPaneCollapsed));
                    RaisePropertyChanged(nameof(PropertiesPaneWidth));
                    RaisePropertyChanged(nameof(RightSplitterWidth));
                }
            }
        }

        public bool IsPropertiesPaneCollapsed => !IsPropertiesPaneVisible;

        public GridLength SeriesPaneWidth =>
            IsSeriesPaneVisible ? new GridLength(DefaultSeriesPaneWidth) : new GridLength(0);

        public GridLength PropertiesPaneWidth =>
            IsPropertiesPaneVisible ? new GridLength(DefaultPropertiesPaneWidth) : new GridLength(0);

        public GridLength LeftSplitterWidth =>
            IsSeriesPaneVisible ? new GridLength(4) : new GridLength(0);

        public GridLength RightSplitterWidth =>
            IsPropertiesPaneVisible ? new GridLength(4) : new GridLength(0);

        private void ToggleSeriesPane()
        {
            IsSeriesPaneVisible = !IsSeriesPaneVisible;
        }

        private void TogglePropertiesPane()
        {
            IsPropertiesPaneVisible = !IsPropertiesPaneVisible;
        }
    }
}