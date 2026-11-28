using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Events;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Enums;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ViewportTileViewModel : BindableBase
    {
        private readonly IImagingMeasurementInstanceResolver _measurementInstanceResolver;

        private readonly IImagingMeasurementSessionStore _measurementSessionStore;

        private readonly string _title;

        private readonly ViewportTransformState _transformState = new();

        private readonly string _viewportId;

        private ViewportMeasurementViewModel? _activeMeasurementDraft, _selectedMeasurement;

        private ImagingTool _activeTool;

        private DicomImageFrame? _currentDicomFrame;

        private string? _currentFileName, _currentFilePath;

        private ImageSource? _currentImage;

        private int? _currentPixelX, _currentPixelY;

        private int _currentSlice = 1, _sliceCount = 1;

        private IReadOnlyList<DicomImagePosition> _imagePositions = [];

        private ImagingMeasurementInstanceKey? _measurementInstanceKey;

        private ObservableCollection<ViewportMeasurementViewModel> _measurements = [];

        private SeriesInfo? _series;

        private IReadOnlyList<string> _seriesFiles = [];

        private double? _windowCenter, _windowWidth, _currentPixelValue;

        public ViewportTileViewModel(string viewportId, string title, IImagingMeasurementSessionStore measurementSessionStore, IImagingMeasurementInstanceResolver measurementInstanceResolver)
        {
            if (string.IsNullOrWhiteSpace(viewportId)) throw new ArgumentException("The viewport ID must not be empty.", nameof(viewportId));
            _viewportId = viewportId;
            _title = title ?? throw new ArgumentNullException(nameof(title));
            _measurementSessionStore = measurementSessionStore ?? throw new ArgumentNullException(nameof(measurementSessionStore));
            _measurementInstanceResolver = measurementInstanceResolver ?? throw new ArgumentNullException(nameof(measurementInstanceResolver));
            _transformState.PropertyChanged += OnTransformStatePropertyChanged;
        }

        public event EventHandler<ViewportSliceNavigationRequestedEventArgs>? SliceNavigationRequested;

        public event EventHandler<ViewportStateChangedEventArgs>? ViewportStateChanged;

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

        public ImagingTool ActiveTool
        {
            get => _activeTool;
            private set
            {
                if (SetProperty(ref _activeTool, value))
                {
                    RaisePropertyChanged(nameof(IsPanActive));
                    RaisePropertyChanged(nameof(IsZoomActive));
                }
            }
        }

        public bool CanZoomIn => _transformState.CanZoomIn;
        public bool CanZoomOut => _transformState.CanZoomOut;

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

        public DicomImagePosition? CurrentImagePosition => CurrentSlice > 0 && CurrentSlice <= ImagePositions.Count ? ImagePositions[CurrentSlice - 1] : null;

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
                    RaisePropertyChanged(nameof(CurrentImagePosition));
                    RaisePropertyChanged(nameof(SliceDisplayText));
                }
            }
        }

        public string DisplaySubtitle => Series == null ? Resources.ViewportTile_Series_None : $"{Series.Modality} · {SliceCount} {Resources.ViewportTile_Images}";
        public string DisplayTitle => Series == null ? Title : Series.Name;
        public IReadOnlyList<DicomImagePosition> ImagePositions { get => _imagePositions; private set => SetProperty(ref _imagePositions, value); }
        public bool IsEmptyViewerVisible => CurrentImage == null;
        public bool IsFlippedHorizontally => _transformState.IsFlippedHorizontally;
        public bool IsFlippedVertically => _transformState.IsFlippedVertically;
        public bool IsPanActive => ActiveTool == ImagingTool.Pan;
        public bool IsZoomActive => ActiveTool == ImagingTool.Zoom;

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

        public double PanOffsetX => _transformState.PanOffsetX;
        public double PanOffsetY => _transformState.PanOffsetY;
        public ViewportRotation Rotation => _transformState.Rotation;

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
                    RaisePropertyChanged(nameof(DisplaySubtitle));
                    RaisePropertyChanged(nameof(SliceDisplayText));
                }
            }
        }

        public string SliceDisplayText => $"{CurrentSlice}/{SliceCount}";
        public string Title => _title;
        public string ViewportId => _viewportId;
        public double? WindowCenter => _windowCenter;
        public string WindowDisplayText => !WindowCenter.HasValue || !WindowWidth.HasValue ? $"{Resources.ViewportTile_WindowLevel}: {Resources.ViewportTile_Default}" : $"{Resources.ViewportTile_WindowLevel}: {WindowWidth.Value:0}/{WindowCenter.Value:0}";
        public double? WindowWidth => _windowWidth;
        public double ZoomFactor => _transformState.ZoomFactor;

        public void AddPan(double horizontalDelta, double verticalDelta)
        {
            if (_transformState.AddPan(horizontalDelta, verticalDelta)) PublishLocal(new ViewportPanChange(ViewportId, PanOffsetX, PanOffsetY, ViewportStateChangeOrigin.Local));
        }

        public void ApplySynchronizedStateChange(ViewportStateChange change)
        {
            ArgumentNullException.ThrowIfNull(change);
            if (change.Origin != ViewportStateChangeOrigin.Synchronization) throw new ArgumentException("Only synchronization-origin changes may be applied to a target viewport.", nameof(change));
            if (string.Equals(change.SourceViewportId, ViewportId, StringComparison.Ordinal)) throw new ArgumentException("A viewport cannot apply its own synchronized state change.", nameof(change));
            switch (change)
            {
                case ViewportSliceChange value: SetSliceCore(value.CurrentSlice); break;
                case ViewportWindowLevelChange value: ApplyWindowLevel(value.WindowCenter, value.WindowWidth, false); break;
                case ViewportZoomChange value: _transformState.SetZoom(value.ZoomFactor); break;
                case ViewportPanChange value: _transformState.SetPan(value.PanOffsetX, value.PanOffsetY); break;
                case ViewportRotationChange value: _transformState.SetRotation(value.Rotation); break;
                case ViewportHorizontalFlipChange value: _transformState.SetHorizontalFlip(value.IsFlippedHorizontally); break;
                case ViewportVerticalFlipChange value: _transformState.SetVerticalFlip(value.IsFlippedVertically); break;
                default: throw new ArgumentOutOfRangeException(nameof(change), change, "The viewport state change type is not supported.");
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

        public void FitToView() => ResetViewTransform();

        public void FlipHorizontal()
        {
            if (_transformState.FlipHorizontal()) PublishLocal(new ViewportHorizontalFlipChange(ViewportId, IsFlippedHorizontally, ViewportStateChangeOrigin.Local));
        }

        public void FlipVertical()
        {
            if (_transformState.FlipVertical()) PublishLocal(new ViewportVerticalFlipChange(ViewportId, IsFlippedVertically, ViewportStateChangeOrigin.Local));
        }

        public void MoveSlice(int delta) => SetSlice(CurrentSlice + delta);

        public void RequestSliceNavigation(int delta)
        {
            if (delta == 0 || SeriesFiles.Count == 0) return;
            SliceNavigationRequested?.Invoke(this, new ViewportSliceNavigationRequestedEventArgs(delta));
        }

        public void ResetFlip()
        {
            if (_transformState.SetHorizontalFlip(false)) PublishLocal(new ViewportHorizontalFlipChange(ViewportId, false, ViewportStateChangeOrigin.Local));
            if (_transformState.SetVerticalFlip(false)) PublishLocal(new ViewportVerticalFlipChange(ViewportId, false, ViewportStateChangeOrigin.Local));
        }

        public void ResetPan()
        {
            if (_transformState.ResetPan()) PublishLocal(new ViewportPanChange(ViewportId, PanOffsetX, PanOffsetY, ViewportStateChangeOrigin.Local));
        }

        public void ResetRotation()
        {
            if (_transformState.ResetRotation()) PublishLocal(new ViewportRotationChange(ViewportId, Rotation, ViewportStateChangeOrigin.Local));
        }

        public void ResetViewTransform() => ResetViewTransformCore(true);

        public void ResetWindowLevel() => ApplyWindowLevel(null, null, true);

        public void RotateLeft()
        {
            if (_transformState.RotateLeft()) PublishLocal(new ViewportRotationChange(ViewportId, Rotation, ViewportStateChangeOrigin.Local));
        }

        public void RotateRight()
        {
            if (_transformState.RotateRight()) PublishLocal(new ViewportRotationChange(ViewportId, Rotation, ViewportStateChangeOrigin.Local));
        }

        public void SelectMeasurement(ViewportMeasurementViewModel? measurement)
        {
            if (measurement != null && !Measurements.Contains(measurement))
            {
                return;
            }

            SelectedMeasurement = measurement;
        }

        public void SetActiveTool(ImagingTool activeTool) => ActiveTool = activeTool;

        public void SetCurrentPixel(int pixelX, int pixelY, double value)
        {
            CurrentPixelX = pixelX;
            CurrentPixelY = pixelY;
            CurrentPixelValue = value;
        }

        public void SetImagePositions(SeriesInfo? series, IReadOnlyList<DicomImagePosition> imagePositions)
        {
            // A newly assigned series starts from a predictable fit-to-view state without changing persisted imaging data.
            ResetViewTransformCore(false);
            ClearMeasurements();
            ClearCurrentPixel();

            Series = series;
            ImagePositions = imagePositions ?? [];
            SeriesFiles = [.. ImagePositions.Select(position => position.FilePath).Distinct(StringComparer.OrdinalIgnoreCase)];
            SliceCount = ImagePositions.Count > 0 ? ImagePositions.Count : Math.Max(1, series?.ImageCount ?? 1);

            CurrentDicomFrame = null;
            CurrentImage = null;

            SetSlice(1);

            if (series == null)
            {
                CurrentFileName = null;
                CurrentFilePath = null;
            }
        }

        public bool SetLocalSlice(int slice)
        {
            var changed = SetSliceCore(slice);
            if (changed) PublishLocal(new ViewportSliceChange(ViewportId, CurrentSlice, ViewportStateChangeOrigin.Local));
            return changed;
        }

        public void SetPan(double panOffsetX, double panOffsetY)
        {
            if (_transformState.SetPan(panOffsetX, panOffsetY)) PublishLocal(new ViewportPanChange(ViewportId, PanOffsetX, PanOffsetY, ViewportStateChangeOrigin.Local));
        }

        public void SetSeries(SeriesInfo? series, IReadOnlyList<string> files) => SetImagePositions(series, files?.Select(filePath => new DicomImagePosition(filePath, null, 0, 1)).ToArray() ?? []);

        public void SetSlice(int slice) => SetSliceCore(slice);

        public void SetWindowLevel(double? windowCenter, double? windowWidth) => ApplyWindowLevel(windowCenter, windowWidth, true);

        public void SetZoomAndPan(double zoomFactor, double panOffsetX, double panOffsetY)
        {
            var (ZoomChanged, PanChanged) = _transformState.SetZoomAndPan(zoomFactor, panOffsetX, panOffsetY);
            if (ZoomChanged) PublishLocal(new ViewportZoomChange(ViewportId, ZoomFactor, ViewportStateChangeOrigin.Local));
            if (PanChanged) PublishLocal(new ViewportPanChange(ViewportId, PanOffsetX, PanOffsetY, ViewportStateChangeOrigin.Local));
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

        public void ZoomIn()
        {
            if (_transformState.ZoomIn()) PublishLocal(new ViewportZoomChange(ViewportId, ZoomFactor, ViewportStateChangeOrigin.Local));
        }

        public void ZoomOut()
        {
            if (_transformState.ZoomOut()) PublishLocal(new ViewportZoomChange(ViewportId, ZoomFactor, ViewportStateChangeOrigin.Local));
        }

        private bool ApplyWindowLevel(double? windowCenter, double? windowWidth, bool publish)
        {
            if (windowCenter.HasValue != windowWidth.HasValue) throw new ArgumentException("Window center and width must either both be set or both be null.");
            if (windowCenter.HasValue && !double.IsFinite(windowCenter.Value)) throw new ArgumentOutOfRangeException(nameof(windowCenter));
            if (windowWidth.HasValue && (!double.IsFinite(windowWidth.Value) || windowWidth.Value <= 0.0)) throw new ArgumentOutOfRangeException(nameof(windowWidth));
            var centerChanged = _windowCenter != windowCenter;
            var widthChanged = _windowWidth != windowWidth;
            if (!centerChanged && !widthChanged) return false;
            // Commit the pair before notifying observers so rendering never sees a mixed window/level state.
            _windowCenter = windowCenter;
            _windowWidth = windowWidth;
            if (centerChanged) RaisePropertyChanged(nameof(WindowCenter));
            if (widthChanged) RaisePropertyChanged(nameof(WindowWidth));
            RaisePropertyChanged(nameof(WindowDisplayText));
            if (publish) PublishLocal(new ViewportWindowLevelChange(ViewportId, WindowCenter, WindowWidth, ViewportStateChangeOrigin.Local));
            return true;
        }

        private void BindMeasurementsToCurrentInstance()
        {
            if (Series is null || string.IsNullOrWhiteSpace(CurrentFilePath))
            {
                _measurementInstanceKey = null;
                Measurements = [];
                return;
            }

            _measurementInstanceKey = _measurementInstanceResolver.Resolve(Series, CurrentFilePath, CurrentImagePosition?.FrameIndex ?? 0);
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

        private void PublishLocal(ViewportStateChange change) => ViewportStateChanged?.Invoke(this, new ViewportStateChangedEventArgs(change));

        private void RaiseMeasurementPropertiesChanged()
        {
            RaisePropertyChanged(nameof(MeasurementDisplayText));
            RaisePropertyChanged(nameof(ActiveMeasurementDraft));
            RaisePropertyChanged(nameof(Measurements));
            RaisePropertyChanged(nameof(SelectedMeasurement));
        }

        private void ResetViewTransformCore(bool publish)
        {
            var before = _transformState.Snapshot;
            _transformState.SetZoom(ViewportImageGeometryCalculator.DefaultZoomFactor);
            _transformState.SetPan(0.0, 0.0);
            _transformState.SetRotation(ViewportRotation.None);
            _transformState.SetHorizontalFlip(false);
            _transformState.SetVerticalFlip(false);
            if (!publish) return;
            if (before.ZoomFactor != ZoomFactor) PublishLocal(new ViewportZoomChange(ViewportId, ZoomFactor, ViewportStateChangeOrigin.Local));
            if (before.PanOffsetX != PanOffsetX || before.PanOffsetY != PanOffsetY) PublishLocal(new ViewportPanChange(ViewportId, PanOffsetX, PanOffsetY, ViewportStateChangeOrigin.Local));
            if (before.Rotation != Rotation) PublishLocal(new ViewportRotationChange(ViewportId, Rotation, ViewportStateChangeOrigin.Local));
            if (before.IsFlippedHorizontally != IsFlippedHorizontally) PublishLocal(new ViewportHorizontalFlipChange(ViewportId, IsFlippedHorizontally, ViewportStateChangeOrigin.Local));
            if (before.IsFlippedVertically != IsFlippedVertically) PublishLocal(new ViewportVerticalFlipChange(ViewportId, IsFlippedVertically, ViewportStateChangeOrigin.Local));
        }

        private bool SetSliceCore(int slice)
        {
            var previousSlice = CurrentSlice;
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
            return previousSlice != CurrentSlice;
        }

        private void UpdateCurrentFile()
        {
            var imagePosition = CurrentImagePosition;

            if (imagePosition == null)
            {
                CurrentFileName = null;
                CurrentFilePath = null;
                return;
            }

            CurrentFilePath = imagePosition.FilePath;
            CurrentFileName = Path.GetFileName(imagePosition.FilePath);
        }
    }
}
