using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class PropertiesPanelViewModel : ViewModelBase
    {
        private readonly ICore _core;

        private readonly IImagingMeasurementContext _measurementContext;
        private DelegateCommand? _clearActiveMeasurementsCommand;
        private int _currentSlice = 1;
        private DelegateCommand? _deleteSelectedMeasurementCommand;
        private SeriesInfo? _selectedSeries;
        private StudyInfo? _selectedStudy;

        public PropertiesPanelViewModel(    ICore core,    IImagingMeasurementContext measurementContext)
        {
            _core = core;
            _measurementContext = measurementContext;

            Properties = [];

            _core.ImagingService!.ImagingSelectionService!.SelectedSeriesChanged += OnSelectedSeriesChanged;
            _core.ImagingService!.ImagingViewportService!.StateChanged += OnViewportStateChanged;
            (_measurementContext as INotifyPropertyChanged)?.PropertyChanged += OnMeasurementContextPropertyChanged;

            _currentSlice = _core.ImagingService!.ImagingViewportService!.State.CurrentSlice;

            ApplySelection(
                _core.ImagingService!.ImagingSelectionService!.SelectedStudy,
                _core.ImagingService!.ImagingSelectionService!.SelectedSeries);
        }

        public ObservableCollection<ViewportMeasurementViewModel> ActiveMeasurements => _measurementContext.ActiveMeasurements;

        public DelegateCommand ClearActiveMeasurementsCommand => _clearActiveMeasurementsCommand ??= new DelegateCommand(ClearActiveMeasurements);

        public DelegateCommand DeleteSelectedMeasurementCommand => _deleteSelectedMeasurementCommand ??= new DelegateCommand(DeleteSelectedMeasurement);

        public bool HasActiveMeasurements => _measurementContext.HasActiveMeasurements;

        public bool HasSelectedMeasurement => _measurementContext.HasSelectedMeasurement;

        public ObservableCollection<PropertyItemViewModel> Properties { get; }

        public ViewportMeasurementViewModel? SelectedMeasurement
        {
            get => _measurementContext.SelectedMeasurement;
            set
            {
                _measurementContext.SelectedMeasurement = value;

                RaisePropertyChanged(nameof(SelectedMeasurement));
                RaisePropertyChanged(nameof(HasSelectedMeasurement));
            }
        }

        public SeriesInfo? SelectedSeries
        {
            get => _selectedSeries;
            private set
            {
                if (SetProperty(ref _selectedSeries, value))
                {
                    RaisePropertyChanged(nameof(Title));
                    RaisePropertyChanged(nameof(Subtitle));
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
                    RaisePropertyChanged(nameof(Title));
                    RaisePropertyChanged(nameof(Subtitle));
                }
            }
        }

        public string Subtitle => SelectedSeries == null ? Resources.PropertiesPanel_Series_None : $"{SelectedSeries.Modality} · {SelectedSeries.ImageCount} {Resources.PropertiesPanel_Images}";

        public string Title => SelectedSeries == null ? Resources.PropertiesPanel_Propterties : SelectedSeries.Name;

        public override void Destroy()
        {
            _core.ImagingService!.ImagingSelectionService!.SelectedSeriesChanged -= OnSelectedSeriesChanged;
            _core.ImagingService!.ImagingViewportService!.StateChanged -= OnViewportStateChanged;
            (_measurementContext as INotifyPropertyChanged)?.PropertyChanged -= OnMeasurementContextPropertyChanged;

            base.Destroy();
        }

        private void AddDicomMetadata(DicomFileMetadata metadata)
        {
            AddSection(Resources.PropertiesPanel_CurrentImage);

            AddProperty(Resources.PropertiesPanel_File, Path.GetFileName(metadata.FilePath));
            AddProperty(Resources.PropertiesPanel_Path, metadata.FilePath);

            if (metadata.InstanceNumber.HasValue)
            {
                AddProperty(Resources.PropertiesPanel_InstanceNumber, metadata.InstanceNumber.Value.ToString());
            }

            AddProperty(Resources.PropertiesPanel_SOPInstanceUID, metadata.SopInstanceUid);

            if (metadata.Rows.HasValue)
            {
                AddProperty(Resources.PropertiesPanel_Rows, metadata.Rows.Value.ToString());
            }

            if (metadata.Columns.HasValue)
            {
                AddProperty(Resources.PropertiesPanel_Colums, metadata.Columns.Value.ToString());
            }

            AddSection(Resources.PropertiesPanel_DicomSeries);

            AddProperty(Resources.PropertiesPanel_Modality, metadata.Modality);
            AddProperty(Resources.PropertiesPanel_StudyDescription, metadata.StudyDescription);
            AddProperty(Resources.PropertiesPanel_SeriesDescription, metadata.SeriesDescription);
            AddProperty(Resources.PropertiesPanel_StudyInstanceUID, metadata.StudyInstanceUid);
            AddProperty(Resources.PropertiesPanel_SeriesInstanceUID, metadata.SeriesInstanceUid);
        }

        private void AddProperty(string name, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                value = "-";
            }

            Properties.Add(new PropertyItemViewModel(name, value, isSection: false));
        }

        private void AddSection(string title) => Properties.Add(new PropertyItemViewModel(name: title, value: string.Empty, isSection: true));

        private void ApplySelection(StudyInfo? study, SeriesInfo? series)
        {
            SelectedStudy = study;
            SelectedSeries = series;

            RebuildProperties();
        }

        private void ClearActiveMeasurements()
        {
            _measurementContext.ClearActiveMeasurements();

            RaiseMeasurementPropertiesChanged();
        }

        private void DeleteSelectedMeasurement()
        {
            _measurementContext.DeleteSelectedMeasurement();

            RaiseMeasurementPropertiesChanged();
        }

        private DicomFileMetadata? GetCurrentDicomMetadata()
        {
            if (SelectedSeries == null)
            {
                return null;
            }

            var metadata = _core.ImagingService!.ImagingStudyService!.GetDicomMetadataForSeries(SelectedSeries.Id);

            if (metadata.Count == 0)
            {
                return null;
            }

            var index = _currentSlice - 1;

            if (index < 0 || index >= metadata.Count)
            {
                return null;
            }

            return metadata[index];
        }

        private void OnMeasurementContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IImagingMeasurementContext.ActiveMeasurements) || e.PropertyName == nameof(IImagingMeasurementContext.SelectedMeasurement) || e.PropertyName == nameof(IImagingMeasurementContext.HasActiveMeasurements) || e.PropertyName == nameof(IImagingMeasurementContext.HasSelectedMeasurement))
            {
                RaiseMeasurementPropertiesChanged();
            }
        }

        private void OnSelectedSeriesChanged(object? sender, SeriesSelectionChangedEventArgs e) => ApplySelection(e.SelectedStudy, e.SelectedSeries);

        private void OnViewportStateChanged(object? sender, ImagingViewportStateChangedEventArgs e)
        {
            if (_currentSlice == e.State.CurrentSlice)
            {
                return;
            }

            _currentSlice = e.State.CurrentSlice;

            RebuildProperties();
        }

        private void RaiseMeasurementPropertiesChanged()
        {
            RaisePropertyChanged(nameof(ActiveMeasurements));
            RaisePropertyChanged(nameof(SelectedMeasurement));
            RaisePropertyChanged(nameof(HasActiveMeasurements));
            RaisePropertyChanged(nameof(HasSelectedMeasurement));
        }

        private void RebuildProperties()
        {
            Properties.Clear();

            if (SelectedSeries == null)
            {
                AddProperty(Resources.PropertiesPanel_Status, Resources.PropertiesPanel_Series_None);
                return;
            }

            AddSection(Resources.PropertiesPanel_Series);

            AddProperty(Resources.PropertiesPanel_Name, SelectedSeries.Name);
            AddProperty(Resources.PropertiesPanel_Modality, SelectedSeries.Modality);
            AddProperty(Resources.PropertiesPanel_Description, SelectedSeries.Description);
            AddProperty(Resources.PropertiesPanel_Images, SelectedSeries.ImageCount.ToString());

            if (SelectedSeries.SeriesNumber.HasValue)
            {
                AddProperty(Resources.PropertiesPanel_SeriesNumber, SelectedSeries.SeriesNumber.Value.ToString());
            }

            AddProperty(Resources.PropertiesPanel_SeriesId, SelectedSeries.Id);

            if (!string.IsNullOrWhiteSpace(SelectedSeries.StudyId))
            {
                AddProperty(Resources.PropertiesPanel_StudyId, SelectedSeries.StudyId);
            }

            var files = _core.ImagingService!.ImagingStudyService!.GetFilesForSeries(SelectedSeries.Id);

            AddProperty(Resources.PropertiesPanel_Files, files.Count.ToString());

            var currentMetadata = GetCurrentDicomMetadata();

            if (currentMetadata != null)
            {
                AddDicomMetadata(currentMetadata);
            }
        }
    }
}