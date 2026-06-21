using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class AnnotationItemViewModel(string displayText) : BindableBase
    {
        public string DisplayText { get; } = displayText;
    }

    public sealed class MeasurementItemViewModel(string name, string value) : BindableBase
    {
        public string Name { get; } = name;

        public string Value { get; } = value;
    }

    public sealed class PropertiesPanelViewModel : ViewModelBase
    {
        private readonly ICore _core;

        private bool _isApplyingViewportState;
        private double _levelValue = 40;
        private SeriesInfo? _selectedSeries;
        private double _windowValue = 400;

        public PropertiesPanelViewModel(ICore core)
        {
            _core = core;

            _core.ImagingService!.ImagingSelectionService!.SelectedSeriesChanged += OnSelectedSeriesChanged;
            _core.ImagingService!.ImagingViewportService!.StateChanged += OnViewportStateChanged;

            ResetWindowLevelCommand = new DelegateCommand(ResetWindowLevel);

            AddDistanceMeasurementCommand = new DelegateCommand(AddDistanceMeasurement);
            AddAngleMeasurementCommand = new DelegateCommand(AddAngleMeasurement);
            AddRoiMeasurementCommand = new DelegateCommand(AddRoiMeasurement);

            AddTextAnnotationCommand = new DelegateCommand(AddTextAnnotation);
            AddArrowAnnotationCommand = new DelegateCommand(AddArrowAnnotation);
            AddMarkerAnnotationCommand = new DelegateCommand(AddMarkerAnnotation);

            Measurements =
            [
                new MeasurementItemViewModel("Distanz 1", "23,4 mm"),
                new MeasurementItemViewModel("Winkel 1", "42°")
            ];

            Annotations =
            [
                new AnnotationItemViewModel("Marker: Verdächtiger Bereich"),
                new AnnotationItemViewModel("Text: Kontrolle empfohlen")
            ];

            DicomTags = [];

            ApplySelectedSeries(_core.ImagingService!.ImagingSelectionService!.SelectedSeries);
            ApplyViewportState(_core.ImagingService!.ImagingViewportService!.State);
        }

        public DelegateCommand AddAngleMeasurementCommand { get; }

        public DelegateCommand AddArrowAnnotationCommand { get; }

        public DelegateCommand AddDistanceMeasurementCommand { get; }

        public DelegateCommand AddMarkerAnnotationCommand { get; }

        public DelegateCommand AddRoiMeasurementCommand { get; }

        public DelegateCommand AddTextAnnotationCommand { get; }

        public ObservableCollection<AnnotationItemViewModel> Annotations { get; }

        public ObservableCollection<DicomTagInfo> DicomTags { get; }

        public string LevelDisplayText => LevelValue.ToString("0");

        public double LevelValue
        {
            get => _levelValue;
            set
            {
                if (SetProperty(ref _levelValue, value))
                {
                    RaisePropertyChanged(nameof(LevelDisplayText));
                    UpdateViewportWindowLevel();
                }
            }
        }

        public ObservableCollection<MeasurementItemViewModel> Measurements { get; }

        public DelegateCommand ResetWindowLevelCommand { get; }

        public SeriesInfo? SelectedSeries
        {
            get => _selectedSeries;
            private set => SetProperty(ref _selectedSeries, value);
        }

        public string WindowDisplayText => WindowValue.ToString("0");

        public double WindowValue
        {
            get => _windowValue;
            set
            {
                if (SetProperty(ref _windowValue, value))
                {
                    RaisePropertyChanged(nameof(WindowDisplayText));
                    UpdateViewportWindowLevel();
                }
            }
        }

        public override void Destroy()
        {
            _core.ImagingService!.ImagingSelectionService!.SelectedSeriesChanged -= OnSelectedSeriesChanged;
            _core.ImagingService!.ImagingViewportService!.StateChanged -= OnViewportStateChanged;

            base.Destroy();
        }

        private void AddAngleMeasurement() => Measurements.Add(new MeasurementItemViewModel($"Winkel {Measurements.Count + 1}", "0°"));

        private void AddArrowAnnotation() => Annotations.Add(new AnnotationItemViewModel("Pfeilannotation"));

        private void AddDistanceMeasurement() => Measurements.Add(new MeasurementItemViewModel($"Distanz {Measurements.Count + 1}", "0,0 mm"));

        private void AddMarkerAnnotation() => Annotations.Add(new AnnotationItemViewModel("Markerannotation"));

        private void AddRoiMeasurement() => Measurements.Add(new MeasurementItemViewModel($"ROI {Measurements.Count + 1}", "Mittelwert: -"));

        private void AddTextAnnotation() => Annotations.Add(new AnnotationItemViewModel("Textannotation"));

        private void ApplySelectedSeries(SeriesInfo? series)
        {
            SelectedSeries = series;

            DicomTags.Clear();

            if (series == null)
            {
                DicomTags.Add(new DicomTagInfo("-", "Status", "Keine Serie aktiv"));

                return;
            }

            DicomTags.Add(new DicomTagInfo("(0008,0060)", "Modality", series.Modality));

            DicomTags.Add(new DicomTagInfo("(0020,0011)", "Series Number", series.SeriesNumber?.ToString() ?? "-"));

            DicomTags.Add(new DicomTagInfo("(0020,4000)", "Series Description", series.Description));

            DicomTags.Add(new DicomTagInfo("-", "Series Name", series.Name));

            DicomTags.Add(new DicomTagInfo("-", "Images", series.ImageCount.ToString()));

            if (!string.IsNullOrWhiteSpace(series.StudyId))
            {
                DicomTags.Add(new DicomTagInfo("-", "Study Id", series.StudyId));
            }
        }

        private void ApplyViewportState(ImagingViewportState state)
        {
            _isApplyingViewportState = true;

            try
            {
                WindowValue = state.WindowValue;
                LevelValue = state.LevelValue;
            }
            finally
            {
                _isApplyingViewportState = false;
            }
        }

        private void OnSelectedSeriesChanged(object? sender, SeriesSelectionChangedEventArgs e) => ApplySelectedSeries(e.SelectedSeries);

        private void OnViewportStateChanged(object? sender, ImagingViewportStateChangedEventArgs e) => ApplyViewportState(e.State);

        private void ResetWindowLevel() => _core.ImagingService!.ImagingViewportService!.SetWindowLevel(400, 40);

        private void UpdateViewportWindowLevel()
        {
            if (_isApplyingViewportState)
            {
                return;
            }

            _core.ImagingService!.ImagingViewportService!.SetWindowLevel(WindowValue, LevelValue);
        }
    }
}