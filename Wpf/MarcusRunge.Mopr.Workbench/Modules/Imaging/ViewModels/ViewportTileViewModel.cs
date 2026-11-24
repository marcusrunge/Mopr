using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ViewportTileViewModel : BindableBase
    {
        private readonly IImagingMeasurementInstanceResolver _measurementInstanceResolver;
        private readonly IImagingMeasurementSessionStore _measurementSessionStore;
        private readonly ViewportTransformState _transformState = new();
        private readonly string _title;
        private readonly string _viewportId;
        private ImagingMeasurementInstanceKey? _measurementInstanceKey;
        private ObservableCollection<ViewportMeasurementViewModel> _measurements = [];

        public ViewportTileViewModel(string viewportId, string title, IImagingMeasurementSessionStore measurementSessionStore, IImagingMeasurementInstanceResolver measurementInstanceResolver)
        {
            if (string.IsNullOrWhiteSpace(viewportId)) throw new ArgumentException("The viewport ID must not be empty.", nameof(viewportId));
            _viewportId = viewportId;
            _title = title ?? throw new ArgumentNullException(nameof(title));
            _measurementSessionStore = measurementSessionStore ?? throw new ArgumentNullException(nameof(measurementSessionStore));
            _measurementInstanceResolver = measurementInstanceResolver ?? throw new ArgumentNullException(nameof(measurementInstanceResolver));
            _transformState.PropertyChanged += OnTransformStatePropertyChanged;
        }

        private ViewportMeasurementViewModel? _activeMeasurementDraft, _selectedMeasurement;
        private DicomImageFrame? _currentDicomFrame;
        private string? _currentFileName, _currentFilePath;
        private ImageSource? _currentImage;
        private int? _currentPixelX, _currentPixelY;
        private int _currentSlice = 1, _sliceCount = 1;
        private SeriesInfo? _series;
        private IReadOnlyList<string> _seriesFiles = [];
        private double? _windowCenter, _windowWidth, _currentPixelValue;

        public bool CanZoomIn => _transformState.CanZoomIn;
        public bool CanZoomOut => _transformState.CanZoomOut;
        public double PanOffsetX => _transformState.PanOffsetX;
        public double PanOffsetY => _transformState.PanOffsetY;
        public double ZoomFactor => _transformState.ZoomFactor;

        public ViewportMeasurementViewModel? ActiveMeasurementDraft
        {
            get => _activeMeasurementDraft;
            private set
            {
                if (SetProperty(ref _activeMeasurementDraft, value))
                {
                    RaisePropertyChanged(nameof(MeasurementDisplayText));
                }
            }
        }

        public DicomImageFrame? CurrentDicomFrame
        {
            get => _currentDicomFrame;
            set => SetProperty(ref _currentDicomFrame, value);
        }

        public string CurrentFileDisplayText => string.IsNullOrWhiteSpace(CurrentFileName) ? $"{Resources.ViewportTile_File}: -" : $"{Resources.ViewportTile_File}: {CurrentFileName}";

        public string? CurrentFileName
        {
            get => _currentFileName;
            private set
            {
                if (SetProperty(ref _currentFileName, value))
                {
                    RaisePropertyChanged(nameof(CurrentFileDisplayText));
                    RaisePropertyChanged(nameof(CurrentFileToolTipText));
                }
            }
        }

        public string? CurrentFilePath
        {
            get => _currentFilePath;
            private set
            {
                if (SetProperty(ref _currentFilePath, value))
                {
                    RaisePropertyChanged(nameof(CurrentFileToolTipText));
                }
            }
        }

        public string CurrentFileToolTipText => string.IsNullOrWhiteSpace(CurrentFilePath) ? Resources.ViewportTile_NoFile : $"{Resources.ViewportTile_ActualFile}:{Environment.NewLine}{CurrentFilePath}";

        public ImageSource? CurrentImage
        {
            get => _currentImage;
            set
            {
                if (SetProperty(ref _currentImage, value))
                {
                    RaisePropertyChanged(nameof(IsEmptyViewerVisible));
                }
            }
        }

        public string CurrentPixelDisplayText
        {
            get
            {
                if (!CurrentPixelX.HasValue || !CurrentPixelY.HasValue || !CurrentPixelValue.HasValue)
                {
                    return $"{Resources.ViewportTile_Pixel}: -";
                }

                if (string.Equals(CurrentDicomFrame?.Modality, "CT", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Format(Resources.Status_PixelCtFormat, CurrentPixelX.Value, CurrentPixelY.Value, CurrentPixelValue.Value);
                }

                return string.Format(Resources.Status_PixelValueFormat, CurrentPixelX.Value, CurrentPixelY.Value, CurrentPixelValue.Value);
            }
        }

        public double? CurrentPixelValue
        {
            get => _currentPixelValue;
            private set
            {
                if (SetProperty(ref _currentPixelValue, value))
                {
                    RaisePropertyChanged(nameof(CurrentPixelDisplayText));
                }
            }
        }

        public int? CurrentPixelX
        {
            get => _currentPixelX;
            private set
            {
                if (SetProperty(ref _currentPixelX, value))
                {
                    RaisePropertyChanged(nameof(CurrentPixelDisplayText));
                }
            }
        }

        public int? CurrentPixelY
        {
            get => _currentPixelY;
            private set
            {
                if (SetProperty(ref _currentPixelY, value))
                {
                    RaisePropertyChanged(nameof(CurrentPixelDisplayText));
                }
            }
        }

        public int CurrentSlice
        {
            get => _currentSlice;
            private set
            {
                if (SetProperty(ref _currentSlice, value))
                {
                    RaisePropertyChanged(nameof(SliceDisplayText));
                }
            }
        }

        public string DisplaySubtitle => Series == null ? Resources.ViewportTile_Series_None : $"{Series.Modality} Ã‚Â· {Series.ImageCount} {Resources.ViewportTile_Images}";

        public string DisplayTitle => Series == null ? Title : Series.Name;

        public bool IsEmptyViewerVisible => CurrentImage == null;

        public string MeasurementDisplayText
        {
            get
            {
                if (ActiveMeasurementDraft?.PreviewDistanceMillimeters.HasValue == true)
                {
                    return string.Format(Resources.Status_MeasurementMillimeterFormat, ActiveMeasurementDraft.PreviewDistanceMillimeters.Value);
                }

                if (ActiveMeasurementDraft?.PreviewDistancePixels.HasValue == true)
                {
                    return string.Format(Resources.Status_MeasurementPixelFormat, ActiveMeasurementDraft.PreviewDistancePixels.Value);
                }

                var lastMeasurement = Measurements.Count > 0 ? Measurements[^1] : null;

                if (lastMeasurement == null)
                {
                    return Resources.Status_MeasurementEmpty;
                }

                if (lastMeasurement.DistanceMillimeters.HasValue)
                {
                    return string.Format(Resources.Status_MeasurementMillimeterFormat, lastMeasurement.DistanceMillimeters.Value);
                }

                if (lastMeasurement.DistancePixels.HasValue)
                {
                    return string.Format(Resources.Status_MeasurementPixelFormat, lastMeasurement.DistancePixels.Value);
                }

                return Resources.Status_MeasurementEmpty;
            }
        }

        public ObservableCollection<ViewportMeasurementViewModel> Measurements
        {
            get => _measurements;
            private set
            {
                if (ReferenceEquals(_measurements, value)) return;
                _measurements = value;
                RaisePropertyChanged();
                RaiseMeasurementPropertiesChanged();
            }
        }

        public ViewportMeasurementViewModel? SelectedMeasurement
        {
            get => _selectedMeasurement;
            private set
            {
                if (ReferenceEquals(_selectedMeasurement, value))
                {
                    return;
                }

                _selectedMeasurement?.IsSelected = false;

                _selectedMeasurement = value;

                _selectedMeasurement?.IsSelected = true;

                RaisePropertyChanged(nameof(SelectedMeasurement));
            }
        }

        public SeriesInfo? Series
        {
            get => _series;
            private set
            {
                if (SetProperty(ref _series, value))
                {
                    RaisePropertyChanged(nameof(DisplayTitle));
                    RaisePropertyChanged(nameof(DisplaySubtitle));
                }
            }
        }

        public IReadOnlyList<string> SeriesFiles { get => _seriesFiles; private set => SetProperty(ref _seriesFiles, value); }

        public int SliceCount
        {
            get => _sliceCount;
            private set
            {
                if (SetProperty(ref _sliceCount, value))
                {
                    RaisePropertyChanged(nameof(SliceDisplayText));
                }
            }
        }

        public string SliceDisplayText => $"{CurrentSlice}/{SliceCount}";

        public string Title => _title;

        public string ViewportId => _viewportId;

        public double? WindowCenter
        {
            get => _windowCenter;
            private set
            {
                if (SetProperty(ref _windowCenter, value))
                {
                    RaisePropertyChanged(nameof(WindowDisplayText));
                }
            }
        }

        public string WindowDisplayText
        {
            get
            {
                if (!WindowCenter.HasValue || !WindowWidth.HasValue)
                {
                    return $"{Resources.ViewportTile_WindowLevel}: {Resources.ViewportTile_Default}";
                }

                return $"{Resources.ViewportTile_WindowLevel}: {WindowWidth.Value:0}/{WindowCenter.Value:0}";
            }
        }

        public double? WindowWidth
        {
            get => _windowWidth;
            private set
            {
                if (SetProperty(ref _windowWidth, value))
                {
                    RaisePropertyChanged(nameof(WindowDisplayText));
                }
            }
        }

        public void BeginOrCompleteMeasurement(double imageX, double imageY)
        {
            if (ActiveMeasurementDraft == null)
            {
                ActiveMeasurementDraft = new ViewportMeasurementViewModel(imageX, imageY);

                ActiveMeasurementDraft.SetPixelSpacing(CurrentDicomFrame?.PixelSpacingX, CurrentDicomFrame?.PixelSpacingY);

                RaiseMeasurementPropertiesChanged();

                return;
            }

            ActiveMeasurementDraft.SetPixelSpacing(CurrentDicomFrame?.PixelSpacingX, CurrentDicomFrame?.PixelSpacingY);

            ActiveMeasurementDraft.Complete(imageX, imageY);

            var completedMeasurement = ActiveMeasurementDraft;

            ImagingMeasurementInstanceKey instanceKey = _measurementInstanceKey ?? throw new InvalidOperationException("The displayed DICOM instance is not resolved for measurement creation.");
            _measurementSessionStore.Create(instanceKey, completedMeasurement);

            ActiveMeasurementDraft = null;

            SelectedMeasurement = completedMeasurement;

            RaiseMeasurementPropertiesChanged();
        }

        public void ClearCurrentPixel()
        {
            CurrentPixelX = null;
            CurrentPixelY = null;
            CurrentPixelValue = null;
        }

        public void ClearMeasurements()
        {
            SelectedMeasurement = null;
            ActiveMeasurementDraft = null;
            _measurementInstanceKey = null;
            Measurements = [];
            RaiseMeasurementPropertiesChanged();
        }

        public void DeleteMeasurements(IEnumerable<ViewportMeasurementViewModel> measurements)
        {
            if (measurements == null)
            {
                return;
            }

            var measurementsToRemove = new List<ViewportMeasurementViewModel>();

            foreach (var measurement in measurements)
            {
                if (measurement != null && Measurements.Contains(measurement) && !measurementsToRemove.Contains(measurement))
                {
                    measurementsToRemove.Add(measurement);
                }
            }

            if (measurementsToRemove.Count == 0)
            {
                return;
            }

            if (SelectedMeasurement != null && measurementsToRemove.Contains(SelectedMeasurement))
            {
                SelectedMeasurement = null;
            }

            ImagingMeasurementInstanceKey instanceKey = _measurementInstanceKey ?? throw new InvalidOperationException("The displayed DICOM instance is not resolved for measurement deletion.");
            foreach (var measurement in measurementsToRemove) _measurementSessionStore.Delete(instanceKey, measurement);

            RaiseMeasurementPropertiesChanged();
        }

        public void AddPan(double horizontalDelta, double verticalDelta) => _transformState.AddPan(horizontalDelta, verticalDelta);

        public void FitToView() => _transformState.Reset();

        public void MoveSlice(int delta) => SetSlice(CurrentSlice + delta);

        public void ResetPan() => _transformState.ResetPan();

        public void ResetViewTransform() => _transformState.Reset();

        public void SetPan(double panOffsetX, double panOffsetY) => _transformState.SetPan(panOffsetX, panOffsetY);

        public void SetZoomAndPan(double zoomFactor, double panOffsetX, double panOffsetY) => _transformState.SetZoomAndPan(zoomFactor, panOffsetX, panOffsetY);

        public void ZoomIn() => _transformState.ZoomIn();

        public void ZoomOut() => _transformState.ZoomOut();

        public void ResetWindowLevel()
        {
            WindowCenter = null;
            WindowWidth = null;
        }

        public void SelectMeasurement(ViewportMeasurementViewModel? measurement)
        {
            if (measurement != null && !Measurements.Contains(measurement))
            {
                return;
            }

            SelectedMeasurement = measurement;
        }

        public void SetCurrentPixel(int pixelX, int pixelY, double value)
        {
            CurrentPixelX = pixelX;
            CurrentPixelY = pixelY;
            CurrentPixelValue = value;
        }

        public void SetSeries(SeriesInfo? series, IReadOnlyList<string> files)
        {
            // A newly assigned series starts from a predictable fit-to-view state without changing persisted imaging data.
            ResetViewTransform();
            ClearMeasurements();
            ClearCurrentPixel();

            Series = series;
            SeriesFiles = files ?? [];

            SliceCount = SeriesFiles.Count > 0 ? SeriesFiles.Count : Math.Max(1, series?.ImageCount ?? 1);

            CurrentDicomFrame = null;
            CurrentImage = null;

            SetSlice(1);

            if (series == null)
            {
                CurrentFileName = null;
                CurrentFilePath = null;
            }
        }

        public void SetSlice(int slice)
        {
            if (SliceCount <= 0)
            {
                SliceCount = 1;
            }

            if (slice < 1)
            {
                slice = 1;
            }

            if (slice > SliceCount)
            {
                slice = SliceCount;
            }

            if (CurrentSlice != slice)
            {
                CurrentDicomFrame = null;
                CurrentImage = null;

                ClearCurrentPixel();
                SelectedMeasurement = null;
                ActiveMeasurementDraft = null;
            }

            CurrentSlice = slice;

            UpdateCurrentFile();
            BindMeasurementsToCurrentInstance();
        }

        public void SetWindowLevel(double? windowCenter, double? windowWidth)
        {
            WindowCenter = windowCenter;
            WindowWidth = windowWidth;
        }

        public void UpdateMeasurementDraft(double imageX, double imageY)
        {
            if (ActiveMeasurementDraft == null)
            {
                return;
            }

            ActiveMeasurementDraft.SetPreviewEndPoint(imageX, imageY);

            RaiseMeasurementPropertiesChanged();
        }

        private void BindMeasurementsToCurrentInstance()
        {
            if (Series is null || string.IsNullOrWhiteSpace(CurrentFilePath))
            {
                _measurementInstanceKey = null;
                Measurements = [];
                return;
            }

            _measurementInstanceKey = _measurementInstanceResolver.Resolve(Series, CurrentFilePath);
            Measurements = _measurementSessionStore.GetMeasurements(_measurementInstanceKey);
        }

        private void OnTransformStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs eventArgs)
        {
            if (string.IsNullOrWhiteSpace(eventArgs.PropertyName))
            {
                RaisePropertyChanged(nameof(ZoomFactor));
                RaisePropertyChanged(nameof(PanOffsetX));
                RaisePropertyChanged(nameof(PanOffsetY));
                RaisePropertyChanged(nameof(CanZoomIn));
                RaisePropertyChanged(nameof(CanZoomOut));
                return;
            }

            RaisePropertyChanged(eventArgs.PropertyName);
        }

        private void RaiseMeasurementPropertiesChanged()
        {
            RaisePropertyChanged(nameof(MeasurementDisplayText));
            RaisePropertyChanged(nameof(ActiveMeasurementDraft));
            RaisePropertyChanged(nameof(Measurements));
            RaisePropertyChanged(nameof(SelectedMeasurement));
        }

        private void UpdateCurrentFile()
        {
            if (SeriesFiles.Count == 0)
            {
                CurrentFileName = null;
                CurrentFilePath = null;
                return;
            }

            var index = CurrentSlice - 1;

            if (index < 0 || index >= SeriesFiles.Count)
            {
                CurrentFileName = null;
                CurrentFilePath = null;
                return;
            }

            var filePath = SeriesFiles[index];

            CurrentFilePath = filePath;
            CurrentFileName = Path.GetFileName(filePath);
        }
    }
}