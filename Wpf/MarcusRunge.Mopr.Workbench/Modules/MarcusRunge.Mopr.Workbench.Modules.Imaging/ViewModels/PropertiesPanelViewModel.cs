using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{

    public sealed class PropertiesPanelViewModel : ViewModelBase
    {
        private double _windowValue = 400;
        private double _levelValue = 40;

        public PropertiesPanelViewModel()
        {
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

            DicomTags =
            [
                new DicomTagItemViewModel("(0008,0060)", "Modality", "MR"),
                new DicomTagItemViewModel("(0020,0011)", "Series Number", "3"),
                new DicomTagItemViewModel("(0020,0013)", "Instance Number", "1"),
                new DicomTagItemViewModel("(0018,0050)", "Slice Thickness", "1.0")
            ];
        }

        public double WindowValue
        {
            get => _windowValue;
            set
            {
                if (SetProperty(ref _windowValue, value))
                {
                    RaisePropertyChanged(nameof(WindowDisplayText));
                }
            }
        }

        public double LevelValue
        {
            get => _levelValue;
            set
            {
                if (SetProperty(ref _levelValue, value))
                {
                    RaisePropertyChanged(nameof(LevelDisplayText));
                }
            }
        }

        public string WindowDisplayText => WindowValue.ToString("0");

        public string LevelDisplayText => LevelValue.ToString("0");

        public ObservableCollection<MeasurementItemViewModel> Measurements { get; }

        public ObservableCollection<AnnotationItemViewModel> Annotations { get; }

        public ObservableCollection<DicomTagItemViewModel> DicomTags { get; }

        public DelegateCommand ResetWindowLevelCommand { get; }

        public DelegateCommand AddDistanceMeasurementCommand { get; }

        public DelegateCommand AddAngleMeasurementCommand { get; }

        public DelegateCommand AddRoiMeasurementCommand { get; }

        public DelegateCommand AddTextAnnotationCommand { get; }

        public DelegateCommand AddArrowAnnotationCommand { get; }

        public DelegateCommand AddMarkerAnnotationCommand { get; }

        private void ResetWindowLevel()
        {
            WindowValue = 400;
            LevelValue = 40;
        }

        private void AddDistanceMeasurement()
        {
            Measurements.Add(new MeasurementItemViewModel(
                $"Distanz {Measurements.Count + 1}",
                "0,0 mm"));
        }

        private void AddAngleMeasurement()
        {
            Measurements.Add(new MeasurementItemViewModel(
                $"Winkel {Measurements.Count + 1}",
                "0°"));
        }

        private void AddRoiMeasurement()
        {
            Measurements.Add(new MeasurementItemViewModel(
                $"ROI {Measurements.Count + 1}",
                "Mittelwert: -"));
        }

        private void AddTextAnnotation()
        {
            Annotations.Add(new AnnotationItemViewModel("Textannotation"));
        }

        private void AddArrowAnnotation()
        {
            Annotations.Add(new AnnotationItemViewModel("Pfeilannotation"));
        }

        private void AddMarkerAnnotation()
        {
            Annotations.Add(new AnnotationItemViewModel("Markerannotation"));
        }
    }

    public sealed class MeasurementItemViewModel(string name, string value) : BindableBase
    {
        public string Name { get; } = name;

        public string Value { get; } = value;
    }

    public sealed class AnnotationItemViewModel(string displayText) : BindableBase
    {
        public string DisplayText { get; } = displayText;
    }

    public sealed class DicomTagItemViewModel(string tag, string name, string value) : BindableBase
    {
        public string Tag { get; } = tag;

        public string Name { get; } = name;

        public string Value { get; } = value;
    }
}