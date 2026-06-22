using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using Prism.Commands;
using System.Windows;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ImagingWorkbenchViewModel : ViewModelBase
    {
        private const double CollapsedPaneWidth = 48;
        private const double DefaultPropertiesPaneWidth = 340;
        private const double DefaultSeriesPaneWidth = 280;
        private const double SplitterWidth = 4;

        private readonly ICore _core;

        private bool _isPropertiesPaneVisible = true;
        private bool _isSeriesPaneVisible = true;
        private SeriesInfo? _selectedSeries;
        private StudyInfo? _selectedStudy;

        public ImagingWorkbenchViewModel(ICore core)
        {
            _core = core;

            _core.ImagingService!.ImagingSelectionService!.SelectedSeriesChanged += OnSelectedSeriesChanged;

            ToggleSeriesPaneCommand = new DelegateCommand(ToggleSeriesPane);
            TogglePropertiesPaneCommand = new DelegateCommand(TogglePropertiesPane);

            ApplySelection(_core.ImagingService!.ImagingSelectionService!.SelectedStudy, _core.ImagingService!.ImagingSelectionService!.SelectedSeries);
        }

        public string CurrentSeriesDisplayText => SelectedSeries == null ? "Serie: Keine Serie aktiv" : $"Serie: {SelectedSeries.Name}";

        public string CurrentStudyDisplayText => SelectedStudy == null ? "Studie: Keine Studie geöffnet" : $"Studie: {SelectedStudy.Name}";

        public bool IsPropertiesPaneCollapsed => !IsPropertiesPaneVisible;

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

        public bool IsSeriesPaneCollapsed => !IsSeriesPaneVisible;

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

        public GridLength LeftSplitterWidth => IsSeriesPaneVisible ? new GridLength(SplitterWidth) : new GridLength(0);

        public GridLength PropertiesPaneWidth => IsPropertiesPaneVisible ? new GridLength(DefaultPropertiesPaneWidth) : new GridLength(CollapsedPaneWidth);
        public GridLength RightSplitterWidth => IsPropertiesPaneVisible ? new GridLength(SplitterWidth) : new GridLength(0);

        public SeriesInfo? SelectedSeries
        {
            get => _selectedSeries;
            private set
            {
                if (SetProperty(ref _selectedSeries, value))
                {
                    RaisePropertyChanged(nameof(CurrentSeriesDisplayText));
                }
            }
        }

        public StudyInfo? SelectedStudy
        {
            get => _selectedStudy;
            private set
            {
                if (SetProperty(ref _selectedStudy, value))
                {
                    RaisePropertyChanged(nameof(CurrentStudyDisplayText));
                }
            }
        }

        public GridLength SeriesPaneWidth => IsSeriesPaneVisible ? new GridLength(DefaultSeriesPaneWidth) : new GridLength(CollapsedPaneWidth);
        public DelegateCommand TogglePropertiesPaneCommand { get; }

        public DelegateCommand ToggleSeriesPaneCommand { get; }

        public override void Destroy()
        {
            _core.ImagingService!.ImagingSelectionService!.SelectedSeriesChanged -= OnSelectedSeriesChanged;

            base.Destroy();
        }

        private void ApplySelection(StudyInfo? study, SeriesInfo? series)
        {
            SelectedStudy = study;
            SelectedSeries = series;
        }

        private void OnSelectedSeriesChanged(object? sender, SeriesSelectionChangedEventArgs e) => ApplySelection(e.SelectedStudy, e.SelectedSeries);

        private void TogglePropertiesPane() => IsPropertiesPaneVisible = !IsPropertiesPaneVisible;

        private void ToggleSeriesPane() => IsSeriesPaneVisible = !IsSeriesPaneVisible;
    }
}