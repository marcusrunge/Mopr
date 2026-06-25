using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using System.Collections.ObjectModel;
using System.IO;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class PropertiesPanelViewModel : ViewModelBase
    {
        private readonly ICore _core;

        private int _currentSlice = 1;
        private SeriesInfo? _selectedSeries;
        private StudyInfo? _selectedStudy;

        public PropertiesPanelViewModel(ICore core)
        {
            _core = core;

            Properties = [];

            _core.ImagingService!.ImagingSelectionService!.SelectedSeriesChanged += OnSelectedSeriesChanged;
            _core.ImagingService!.ImagingViewportService!.StateChanged += OnViewportStateChanged;

            _currentSlice = _core.ImagingService!.ImagingViewportService!.State.CurrentSlice;

            ApplySelection(_core.ImagingService!.ImagingSelectionService!.SelectedStudy, _core.ImagingService!.ImagingSelectionService!.SelectedSeries);
        }

        public ObservableCollection<PropertyItemViewModel> Properties { get; }

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

        public string Subtitle => SelectedSeries == null ? "Keine Serie aktiv" : $"{SelectedSeries.Modality} · {SelectedSeries.ImageCount} Bilder";

        public string Title => SelectedSeries == null ? "Eigenschaften" : SelectedSeries.Name;

        public override void Destroy()
        {
            _core.ImagingService!.ImagingSelectionService!.SelectedSeriesChanged -= OnSelectedSeriesChanged;
            _core.ImagingService!.ImagingViewportService!.StateChanged -= OnViewportStateChanged;
            base.Destroy();
        }

        private void AddDicomMetadata(DicomFileMetadata metadata)
        {
            AddSection("Aktuelles Bild");

            AddProperty("Datei", Path.GetFileName(metadata.FilePath));
            AddProperty("Pfad", metadata.FilePath);

            if (metadata.InstanceNumber.HasValue)
            {
                AddProperty("InstanceNumber", metadata.InstanceNumber.Value.ToString());
            }

            AddProperty("SOPInstanceUID", metadata.SopInstanceUid);

            if (metadata.Rows.HasValue)
            {
                AddProperty("Rows", metadata.Rows.Value.ToString());
            }

            if (metadata.Columns.HasValue)
            {
                AddProperty("Columns", metadata.Columns.Value.ToString());
            }

            AddSection("DICOM-Serie");

            AddProperty("Modality", metadata.Modality);
            AddProperty("StudyDescription", metadata.StudyDescription);
            AddProperty("SeriesDescription", metadata.SeriesDescription);
            AddProperty("StudyInstanceUID", metadata.StudyInstanceUid);
            AddProperty("SeriesInstanceUID", metadata.SeriesInstanceUid);
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

        private void RebuildProperties()
        {
            Properties.Clear();

            if (SelectedSeries == null)
            {
                AddProperty("Status", "Keine Serie aktiv");
                return;
            }

            AddSection("Serie");

            AddProperty("Name", SelectedSeries.Name);
            AddProperty("Modalität", SelectedSeries.Modality);
            AddProperty("Beschreibung", SelectedSeries.Description);
            AddProperty("Bilder", SelectedSeries.ImageCount.ToString());

            if (SelectedSeries.SeriesNumber.HasValue)
            {
                AddProperty("Seriennummer", SelectedSeries.SeriesNumber.Value.ToString());
            }

            AddProperty("Series Id", SelectedSeries.Id);

            if (!string.IsNullOrWhiteSpace(SelectedSeries.StudyId))
            {
                AddProperty("Study Id", SelectedSeries.StudyId);
            }

            var files = _core.ImagingService!.ImagingStudyService!.GetFilesForSeries(SelectedSeries.Id);

            AddProperty("Dateien", files.Count.ToString());

            var currentMetadata = GetCurrentDicomMetadata();

            if (currentMetadata != null)
            {
                AddDicomMetadata(currentMetadata);
            }
        }
    }
}