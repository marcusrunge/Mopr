using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Enums;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Events;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ImageViewerViewModel : ViewModelBase
    {
        private const int MaxCachedDicomFrames = 128;
        private const int RenderThrottleMilliseconds = 40;
        private readonly ICore _core;
        private readonly IDicom _dicom;
        private readonly IEventAggregator _eventAggregator;
        private readonly SubscriptionToken _viewportTransformCommandSubscription;
        private readonly Dictionary<string, DicomImageFrame> _dicomFrameCache = [with(StringComparer.OrdinalIgnoreCase)];
        private readonly Queue<string> _dicomFrameCacheOrder = new();
        private readonly HashSet<string> _pendingRenderViewportIds = [with(StringComparer.OrdinalIgnoreCase)];
        private readonly System.Windows.Threading.DispatcherTimer _renderThrottleTimer = new();
        private readonly Dictionary<string, ViewportTileViewModel> _viewportTiles = [with(StringComparer.OrdinalIgnoreCase)];
        private readonly IApplication _wpf;
        private IReadOnlyList<string> _activeSeriesFiles = [];
        private ImagingTool _activeTool;
        private string _activeViewportId = "Single.Main";
        private DelegateCommand<string?>? _clearViewportCommand, _selectViewportCommand, _windowLevelDragCompletedCommand, _windowLevelDragStartedCommand;
        private string? _currentFileName, _currentFilePath, _windowLevelDragViewportId;
        private ImageSource? _currentImage;
        private ImagingLayout _currentLayout;
        private int _currentSlice = 1, _sliceCount = 1;
        private CancellationTokenSource? _imageLoadCancellationTokenSource, _measurementHydrationCancellationTokenSource;
        private bool _isSynchronizingSelectionFromViewport;
        private DelegateCommand<KeyEventArgs?>? _keyDownCommand;
        private DelegateCommand<ViewportMeasurementPointInfo?>? _measurementPointCommand, _measurementPreviewCommand;
        private DelegateCommand<ViewportPixelHoverInfo?>? _pixelHoverCommand;
        private SeriesInfo? _selectedSeries;
        private DelegateCommand<ViewportWindowLevelDragInfo?>? _windowLevelDragCommand;
        private double _zoomFactor = 1.0, _windowLevelDragStartCenter, _windowLevelDragStartWidth;
        private readonly IImagingMeasurementContext _measurementContext;
        private readonly IImagingMeasurementHydrationService _measurementHydrationService;
        private readonly IImagingMeasurementInstanceResolver _measurementInstanceResolver;
        private readonly IImagingMeasurementSessionStore _measurementSessionStore;
        private readonly IViewportSynchronizationService _viewportSynchronizationService;

        public ImageViewerViewModel(ICore core, IApplication wpf, IDicom dicom, IImagingMeasurementContext measurementContext, IImagingMeasurementSessionStore measurementSessionStore, IImagingMeasurementInstanceResolver measurementInstanceResolver, IImagingMeasurementHydrationService measurementHydrationService, IViewportSynchronizationService viewportSynchronizationService, IEventAggregator eventAggregator)
        {
            _core = core;
            _wpf = wpf;
            _dicom = dicom;
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _measurementContext = measurementContext;
            _measurementSessionStore = measurementSessionStore ?? throw new ArgumentNullException(nameof(measurementSessionStore));
            _measurementInstanceResolver = measurementInstanceResolver ?? throw new ArgumentNullException(nameof(measurementInstanceResolver));
            _measurementHydrationService = measurementHydrationService ?? throw new ArgumentNullException(nameof(measurementHydrationService));
            _viewportSynchronizationService = viewportSynchronizationService ?? throw new ArgumentNullException(nameof(viewportSynchronizationService));

            _currentLayout = _core.ImagingService!.ImagingLayoutService!.CurrentLayout;

            _core.ImagingService!.ImagingViewportSelectionService!.SetDefaultViewport(GetDefaultViewportIdForLayout(_currentLayout));

            _activeViewportId = _core.ImagingService!.ImagingViewportSelectionService!.ActiveViewportId;

            _renderThrottleTimer.Interval = TimeSpan.FromMilliseconds(RenderThrottleMilliseconds);

            _renderThrottleTimer.Tick += OnRenderThrottleTimerTick;

            _core.ImagingService!.ImagingLayoutService!.CurrentLayoutChanged += OnCurrentLayoutChanged;
            _core.ImagingService!.ImagingSelectionService!.SelectedSeriesChanged += OnSelectedSeriesChanged;
            _core.ImagingService!.ImagingStudyService!.StudyLoaded += OnStudyLoaded;
            _core.ImagingService!.ImagingToolService!.ActiveToolChanged += OnActiveToolChanged;
            _core.ImagingService!.ImagingViewportSelectionService!.ActiveViewportChanged += OnActiveViewportChanged;
            _core.ImagingService!.ImagingViewportService!.StateChanged += OnViewportStateChanged;
            _core.ImagingService!.ImagingWindowLevelService!.WindowLevelChanged += OnWindowLevelChanged;
            _viewportTransformCommandSubscription = _eventAggregator.GetEvent<ViewportTransformCommandRequestedEvent>().Subscribe(OnViewportTransformCommandRequested, ThreadOption.PublisherThread);
            _activeTool = _core.ImagingService!.ImagingToolService!.ActiveTool;

            SingleMainViewport = RegisterViewport("Single.Main", Resources.Viewer_ViewPort_Single_Main);

            TwoByTwoViewport1 = RegisterViewport("TwoByTwo.Viewport1", Resources.Viewer_ViewPort_TwoByTwo_1);
            TwoByTwoViewport2 = RegisterViewport("TwoByTwo.Viewport2", Resources.Viewer_ViewPort_TwoByTwo_2);
            TwoByTwoViewport3 = RegisterViewport("TwoByTwo.Viewport3", Resources.Viewer_ViewPort_TwoByTwo_3);
            TwoByTwoViewport4 = RegisterViewport("TwoByTwo.Viewport4", Resources.Viewer_ViewPort_TwoByTwo_4);

            MprAxialViewport = RegisterViewport("Mpr.Axial", Resources.Viewer_ViewPort_Mpr_Axial);
            MprSagittalViewport = RegisterViewport("Mpr.Sagittal", Resources.Viewer_ViewPort_Mpr_Sagittal);
            MprCoronalViewport = RegisterViewport("Mpr.Coronal", Resources.Viewer_ViewPort_Mpr_Coronal);
            MprPreview3DViewport = RegisterViewport("Mpr.Preview3D", Resources.Viewer_ViewPort_Mpr_Preview3D);

            AscAxialViewport = RegisterViewport("Asc.Axial", Resources.Viewer_ViewPort_Asc_Axial);
            AscSagittalViewport = RegisterViewport("Asc.Sagittal", Resources.Viewer_ViewPort_Asc_Sagittal);
            AscCoronalViewport = RegisterViewport("Asc.Coronal", Resources.Viewer_ViewPort_Asc_Coronal);

            ApplyViewportState(_core.ImagingService!.ImagingViewportService!.State);
            ApplySelectedSeries(_core.ImagingService!.ImagingSelectionService!.SelectedSeries);
        }

        public ObservableCollection<ViewportMeasurementViewModel> ActiveMeasurements => GetActiveViewportTile()?.Measurements ?? [];

        public IReadOnlyList<string> ActiveSeriesFiles
        {
            get => _activeSeriesFiles;
            private set
            {
                if (SetProperty(ref _activeSeriesFiles, value))
                {
                    RaisePropertyChanged(nameof(HasSeriesFiles));
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
                    RaisePropertyChanged(nameof(ActiveToolDisplayText));
                    RaisePropertyChanged(nameof(IsWindowLevelActive));
                    RaisePropertyChanged(nameof(IsMeasureActive));
                }
            }
        }

        public string ActiveToolDisplayText => string.Format(Resources.Viewer_ActiveToolFormat, GetToolDisplayText(ActiveTool));

        public string ActiveViewportDisplayText
        {
            get
            {
                var viewports = _core.ImagingService!.ImagingLayoutService!.GetViewportsForLayout(CurrentLayout);

                ViewportDescriptor? activeViewport = null;

                for (var i = 0; i < viewports.Count; i++)
                {
                    var viewport = viewports[i];

                    if (string.Equals(viewport.Id, ActiveViewportId, StringComparison.Ordinal))
                    {
                        activeViewport = viewport;
                        break;
                    }
                }

                return activeViewport == null ? $"{Resources.Viewer_ViewPort}: -" : $"{Resources.Viewer_ViewPort}: {activeViewport.Title}";
            }
        }

        public string ActiveViewportId
        {
            get => _activeViewportId;
            private set
            {
                if (SetProperty(ref _activeViewportId, value))
                {
                    RaisePropertyChanged(nameof(ActiveViewportDisplayText));
                }
            }
        }

        public ViewportTileViewModel AscAxialViewport { get; }

        public ViewportTileViewModel AscCoronalViewport { get; }

        public ViewportTileViewModel AscSagittalViewport { get; }

        public DelegateCommand<string?> ClearViewportCommand => _clearViewportCommand ??= new DelegateCommand<string?>(ClearViewport);

        public string CurrentFileDisplayText => string.IsNullOrWhiteSpace(CurrentFileName) ? $"{Resources.Viewer_File}: -" : $"{Resources.Viewer_File}: {CurrentFileName}";

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

        public string CurrentFileToolTipText => string.IsNullOrWhiteSpace(CurrentFilePath) ? Resources.Viewer_File_None : $"{Resources.Viewer_File_Actual}:{Environment.NewLine}{CurrentFilePath}";

        public ImageSource? CurrentImage
        {
            get => _currentImage;
            private set
            {
                if (SetProperty(ref _currentImage, value))
                {
                    RaisePropertyChanged(nameof(IsEmptyViewerVisible));
                }
            }
        }

        public ImagingLayout CurrentLayout
        {
            get => _currentLayout;
            private set
            {
                if (SetProperty(ref _currentLayout, value))
                {
                    RaisePropertyChanged(nameof(ActiveViewportDisplayText));
                    RaisePropertyChanged(nameof(IsSingleLayoutVisible));
                    RaisePropertyChanged(nameof(IsTwoByTwoLayoutVisible));
                    RaisePropertyChanged(nameof(IsMprLayoutVisible));
                    RaisePropertyChanged(nameof(IsAxialSagittalCoronalLayoutVisible));
                }
            }
        }

        public int CurrentSlice
        {
            get => _currentSlice;
            set
            {
                var activeTile = GetActiveViewportTile();

                if (activeTile == null)
                {
                    if (SetProperty(ref _currentSlice, value))
                    {
                        RaisePropertyChanged(nameof(SliceDisplayText));
                    }

                    return;
                }

                if (value == activeTile.CurrentSlice)
                {
                    return;
                }

                activeTile.SetSlice(value);

                SyncActiveViewportToViewerState(activeTile);

                TryLoadCurrentImageForViewport(activeTile);
            }
        }

        public bool HasSeriesFiles => ActiveSeriesFiles.Count > 0;

        public bool IsAxialSagittalCoronalLayoutVisible => CurrentLayout == ImagingLayout.AxialSagittalCoronal;

        public bool IsEmptyViewerVisible => CurrentImage == null;

        public bool IsMeasureActive => ActiveTool == ImagingTool.Measure;

        public bool IsMprLayoutVisible => CurrentLayout == ImagingLayout.Mpr;

        public bool IsSingleLayoutVisible => CurrentLayout == ImagingLayout.Single;

        public bool IsTwoByTwoLayoutVisible => CurrentLayout == ImagingLayout.TwoByTwo;

        public bool IsWindowLevelActive => ActiveTool == ImagingTool.WindowLevel;

        public DelegateCommand<KeyEventArgs?> KeyDownCommand => _keyDownCommand ??= new DelegateCommand<KeyEventArgs?>(OnKeyDown);

        public string MeasurementDisplayText => GetActiveViewportTile()?.MeasurementDisplayText ?? Resources.Status_MeasurementEmpty;

        public DelegateCommand<ViewportMeasurementPointInfo?> MeasurementPointCommand => _measurementPointCommand ??= new DelegateCommand<ViewportMeasurementPointInfo?>(OnMeasurementPoint);

        public DelegateCommand<ViewportMeasurementPointInfo?> MeasurementPreviewCommand => _measurementPreviewCommand ??= new DelegateCommand<ViewportMeasurementPointInfo?>(OnMeasurementPreview);

        public ViewportTileViewModel MprAxialViewport { get; }

        public ViewportTileViewModel MprCoronalViewport { get; }

        public ViewportTileViewModel MprPreview3DViewport { get; }

        public ViewportTileViewModel MprSagittalViewport { get; }

        public string PixelDisplayText => GetActiveViewportTile()?.CurrentPixelDisplayText ?? $"{Resources.Viewer_Pixel}: -";

        public DelegateCommand<ViewportPixelHoverInfo?> PixelHoverCommand => _pixelHoverCommand ??= new DelegateCommand<ViewportPixelHoverInfo?>(OnPixelHover);

        public ViewportMeasurementViewModel? SelectedMeasurement
        {
            get => GetActiveViewportTile()?.SelectedMeasurement;
            set
            {
                var activeTile = GetActiveViewportTile();

                if (activeTile == null)
                {
                    return;
                }

                activeTile.SelectMeasurement(value);

                RaisePropertyChanged(nameof(SelectedMeasurement));
                RaisePropertyChanged(nameof(MeasurementDisplayText));
            }
        }

        public SeriesInfo? SelectedSeries
        {
            get => _selectedSeries;
            private set
            {
                if (SetProperty(ref _selectedSeries, value))
                {
                    RaisePropertyChanged(nameof(ViewerTitle));
                    RaisePropertyChanged(nameof(ViewerSubtitle));
                }
            }
        }

        public DelegateCommand<string?> SelectViewportCommand => _selectViewportCommand ??= new DelegateCommand<string?>(SelectViewport);

        public ViewportTileViewModel SingleMainViewport { get; }

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

        public ViewportTileViewModel TwoByTwoViewport1 { get; }

        public ViewportTileViewModel TwoByTwoViewport2 { get; }

        public ViewportTileViewModel TwoByTwoViewport3 { get; }

        public ViewportTileViewModel TwoByTwoViewport4 { get; }

        public string ViewerSubtitle => SelectedSeries == null ? Resources.Viewer_NoSeries : $"{SelectedSeries.Modality} ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚Â· {SelectedSeries.Description}";

        public string ViewerTitle => SelectedSeries == null ? Resources.Viewer_Title : SelectedSeries.Name;

        public string WindowDisplayText => GetActiveViewportTile()?.WindowDisplayText ?? $"{Resources.Viewer_WindowLevel}: {Resources.Viewer_Default}";

        public DelegateCommand<ViewportWindowLevelDragInfo?> WindowLevelDragCommand => _windowLevelDragCommand ??= new DelegateCommand<ViewportWindowLevelDragInfo?>(OnWindowLevelDrag);

        public DelegateCommand<string?> WindowLevelDragCompletedCommand => _windowLevelDragCompletedCommand ??= new DelegateCommand<string?>(OnWindowLevelDragCompleted);

        public DelegateCommand<string?> WindowLevelDragStartedCommand => _windowLevelDragStartedCommand ??= new DelegateCommand<string?>(OnWindowLevelDragStarted);

        public string ZoomDisplayText => $"{ZoomFactor:P0}";

        public double ZoomFactor
        {
            get => _zoomFactor;
            private set
            {
                if (SetProperty(ref _zoomFactor, value))
                {
                    RaisePropertyChanged(nameof(ZoomDisplayText));
                }
            }
        }

        public override void Destroy()
        {
            _core.ImagingService!.ImagingSelectionService!.SelectedSeriesChanged -= OnSelectedSeriesChanged;
            _core.ImagingService!.ImagingToolService!.ActiveToolChanged -= OnActiveToolChanged;
            _core.ImagingService!.ImagingViewportService!.StateChanged -= OnViewportStateChanged;
            _core.ImagingService!.ImagingLayoutService!.CurrentLayoutChanged -= OnCurrentLayoutChanged;
            _core.ImagingService!.ImagingViewportSelectionService!.ActiveViewportChanged -= OnActiveViewportChanged;
            _core.ImagingService!.ImagingWindowLevelService!.WindowLevelChanged -= OnWindowLevelChanged;
            _core.ImagingService!.ImagingStudyService!.StudyLoaded -= OnStudyLoaded;
            _eventAggregator.GetEvent<ViewportTransformCommandRequestedEvent>().Unsubscribe(_viewportTransformCommandSubscription);

            _imageLoadCancellationTokenSource?.Cancel();
            _imageLoadCancellationTokenSource?.Dispose();
            _imageLoadCancellationTokenSource = null;
            _measurementHydrationCancellationTokenSource?.Cancel();
            _measurementHydrationCancellationTokenSource?.Dispose();
            _measurementHydrationCancellationTokenSource = null;

            ClearDicomFrameCache();

            _renderThrottleTimer.Stop();
            _renderThrottleTimer.Tick -= OnRenderThrottleTimerTick;
            _pendingRenderViewportIds.Clear();

            foreach (var tile in _viewportTiles.Values)
            {
                tile.PropertyChanged -= OnViewportTilePropertyChanged;
                tile.SliceNavigationRequested -= OnSliceNavigationRequested;
                tile.ViewportStateChanged -= OnViewportStateChangeRequested;
            }

            base.Destroy();
        }

        private static string GetToolDisplayText(ImagingTool tool) => tool switch
        {
            ImagingTool.WindowLevel => Resources.Viewer_Tool_WindowLevel,
            ImagingTool.Zoom => Resources.Viewer_Tool_Zoom,
            ImagingTool.Pan => Resources.Viewer_Tool_Pan,
            ImagingTool.Crosshair => Resources.Viewer_Tool_Crosshair,
            ImagingTool.Measure => Resources.Viewer_Tool_Measure,
            _ => Resources.Viewer_Tool_None
        };

        private void ApplySelectedSeries(SeriesInfo? series)
        {
            var activeTile = GetActiveViewportTile();

            if (activeTile == null)
            {
                return;
            }

            AssignSeriesToViewport(activeTile, series);
        }

        private void ApplyViewportState(ImagingViewportState state)
        {
            ZoomFactor = state.ZoomFactor;

            var activeTile = GetActiveViewportTile();

            if (activeTile == null)
            {
                return;
            }

            if (activeTile.Series == null)
            {
                SyncActiveViewportToViewerState(activeTile);
                return;
            }

            if (state.CurrentSlice == 1 && activeTile.CurrentSlice != 1)
            {
                activeTile.SetSlice(1);

                SyncActiveViewportToViewerState(activeTile);

                TryLoadCurrentImageForViewport(activeTile);
            }
        }

        private void AssignSeriesToViewport(ViewportTileViewModel tile, SeriesInfo? series)
        {
            if (series == null)
            {
                tile.SetSeries(null, []);

                SyncActiveViewportToViewerState(tile);

                return;
            }

            var files = _core.ImagingService!.ImagingStudyService!.GetFilesForSeries(series.Id);

            tile.SetSeries(series, files);

            SyncActiveViewportToViewerState(tile);

            TryLoadCurrentImageForViewport(tile);
        }

        private void ClearActiveMeasurements()
        {
            var activeTile = GetActiveViewportTile();

            if (activeTile == null)
            {
                return;
            }

            activeTile.ClearMeasurements();

            RaisePropertyChanged(nameof(ActiveMeasurements));
            RaisePropertyChanged(nameof(SelectedMeasurement));
            RaisePropertyChanged(nameof(MeasurementDisplayText));
        }

        private void ClearAllViewports(bool syncActiveViewport, bool clearSelection)
        {
            foreach (var tile in _viewportTiles.Values)
            {
                tile.SetSeries(null, []);

                tile.CurrentImage = null;
            }

            if (syncActiveViewport)
            {
                var activeTile = GetActiveViewportTile();

                if (activeTile != null)
                {
                    SyncActiveViewportToViewerState(activeTile);
                }
            }

            if (clearSelection)
            {
                try
                {
                    _isSynchronizingSelectionFromViewport = true;

                    _core.ImagingService!.ImagingSelectionService!.SelectSeries(null);
                }
                finally
                {
                    _isSynchronizingSelectionFromViewport = false;
                }
            }
        }

        private void ClearDicomFrameCache()
        {
            _dicomFrameCache.Clear();
            _dicomFrameCacheOrder.Clear();
        }

        private void ClearViewport(string? viewportId)
        {
            if (string.IsNullOrWhiteSpace(viewportId))
            {
                viewportId = ActiveViewportId;
            }

            if (!_viewportTiles.TryGetValue(viewportId, out var tile))
            {
                return;
            }

            tile.SetSeries(null, []);

            if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal))
            {
                SyncActiveViewportToViewerState(tile);

                try
                {
                    _isSynchronizingSelectionFromViewport = true;

                    _core.ImagingService!.ImagingSelectionService!.SelectSeries(null);
                }
                finally
                {
                    _isSynchronizingSelectionFromViewport = false;
                }
            }
        }

        private ViewportTileViewModel? GetActiveViewportTile() => _viewportTiles.TryGetValue(ActiveViewportId, out var tile) ? tile : null;

        private string GetDefaultViewportIdForLayout(ImagingLayout layout)
        {
            var viewports = _core.ImagingService!.ImagingLayoutService!.GetViewportsForLayout(layout);

            ViewportDescriptor? defaultViewport = null;

            for (var i = 0; i < viewports.Count; i++)
            {
                if (viewports[i].IsInteractive)
                {
                    defaultViewport = viewports[i];
                    break;
                }
            }

            if (defaultViewport == null && viewports.Count > 0)
            {
                defaultViewport = viewports[0];
            }

            return defaultViewport?.Id ?? "Single.Main";
        }

        private async Task<DicomImageFrame?> GetOrLoadDicomFrameAsync(string filePath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            if (_dicomFrameCache.TryGetValue(filePath, out var cachedFrame))
            {
                return cachedFrame;
            }

            var frame = await _dicom.ImageService!.LoadImageFrameAsync(filePath, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (frame == null)
            {
                return null;
            }

            _dicomFrameCache[filePath] = frame;
            _dicomFrameCacheOrder.Enqueue(filePath);

            TrimDicomFrameCache();

            return frame;
        }

        private void MoveSlice(int delta)
        {
            var activeTile = GetActiveViewportTile();
            if (activeTile == null) return;
            MoveSlice(activeTile, delta);
        }

        private void MoveSlice(ViewportTileViewModel tile, int delta)
        {
            tile.MoveSlice(delta);
            SyncActiveViewportToViewerState(tile);
            TryLoadCurrentImageForViewport(tile);
        }

        private void MoveToFirstSlice()
        {
            var activeTile = GetActiveViewportTile();

            if (activeTile == null)
            {
                return;
            }

            activeTile.SetSlice(1);

            SyncActiveViewportToViewerState(activeTile);

            TryLoadCurrentImageForViewport(activeTile);
        }

        private void MoveToLastSlice()
        {
            var activeTile = GetActiveViewportTile();

            if (activeTile == null)
            {
                return;
            }

            activeTile.SetSlice(activeTile.SliceCount);

            SyncActiveViewportToViewerState(activeTile);

            TryLoadCurrentImageForViewport(activeTile);
        }

        private void OnViewportStateChangeRequested(object? sender, ViewportStateChangedEventArgs e)
        {
            if (sender is not ViewportTileViewModel sourceTile || e.Change.Origin != ViewportStateChangeOrigin.Local || !string.Equals(sourceTile.ViewportId, e.Change.SourceViewportId, StringComparison.Ordinal)) return;

            foreach (var route in _viewportSynchronizationService.GetRoutes(sourceTile.ViewportId, e.Change.SynchronizationType))
            {
                if (!_viewportTiles.TryGetValue(route.TargetViewportId, out var targetTile)) continue;
                targetTile.ApplySynchronizedStateChange(CreateSynchronizedChange(e.Change));
                if (e.Change.SynchronizationType == ViewportSynchronizationFlags.WindowLevel) RequestRenderCurrentFrameForViewport(targetTile);
            }
        }

        private static ViewportStateChange CreateSynchronizedChange(ViewportStateChange change) => change switch
        {
            ViewportWindowLevelChange value => new ViewportWindowLevelChange(value.SourceViewportId, value.WindowCenter, value.WindowWidth, ViewportStateChangeOrigin.Synchronization),
            ViewportZoomChange value => new ViewportZoomChange(value.SourceViewportId, value.ZoomFactor, ViewportStateChangeOrigin.Synchronization),
            ViewportPanChange value => new ViewportPanChange(value.SourceViewportId, value.PanOffsetX, value.PanOffsetY, ViewportStateChangeOrigin.Synchronization),
            ViewportRotationChange value => new ViewportRotationChange(value.SourceViewportId, value.Rotation, ViewportStateChangeOrigin.Synchronization),
            ViewportHorizontalFlipChange value => new ViewportHorizontalFlipChange(value.SourceViewportId, value.IsFlippedHorizontally, ViewportStateChangeOrigin.Synchronization),
            ViewportVerticalFlipChange value => new ViewportVerticalFlipChange(value.SourceViewportId, value.IsFlippedVertically, ViewportStateChangeOrigin.Synchronization),
            _ => throw new ArgumentOutOfRangeException(nameof(change), change, "The viewport state change type is not supported.")
        };

        private void OnViewportTilePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not ViewportTileViewModel tile || !string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal)) return;
            if (string.Equals(e.PropertyName, nameof(ViewportTileViewModel.ZoomFactor), StringComparison.Ordinal)) ZoomFactor = tile.ZoomFactor;
        }

        private void OnSliceNavigationRequested(object? sender, ViewportSliceNavigationRequestedEventArgs e)
        {
            if (sender is not ViewportTileViewModel tile || e.Delta == 0) return;
            MoveSlice(tile, e.Delta);
        }

        private void OnActiveToolChanged(object? sender, ImagingToolChangedEventArgs e)
        {
            ActiveTool = e.NewTool;

            // Every tile receives the same selected tool while retaining its own independent transform state.
            foreach (var tile in _viewportTiles.Values) tile.SetActiveTool(e.NewTool);
        }

        private void OnActiveViewportChanged(object? sender, ImagingViewportSelectionChangedEventArgs e)
        {
            ActiveViewportId = e.NewViewportId;

            var activeTile = GetActiveViewportTile();

            if (activeTile == null)
            {
                return;
            }

            SyncActiveViewportToViewerState(activeTile);

            if (activeTile.CurrentImage == null && !string.IsNullOrWhiteSpace(activeTile.CurrentFilePath))
            {
                TryLoadCurrentImageForViewport(activeTile);
            }

            try
            {
                _isSynchronizingSelectionFromViewport = true;

                _core.ImagingService!.ImagingSelectionService!.SelectSeries(activeTile.Series);
            }
            finally
            {
                _isSynchronizingSelectionFromViewport = false;
            }
        }

        private void OnCurrentLayoutChanged(object? sender, ImagingLayoutChangedEventArgs e)
        {
            CurrentLayout = e.NewLayout;

            var defaultViewportId = GetDefaultViewportIdForLayout(e.NewLayout);

            _core.ImagingService!.ImagingViewportSelectionService!.SetDefaultViewport(defaultViewportId);

            ActiveViewportId = _core.ImagingService!.ImagingViewportSelectionService!.ActiveViewportId;

            var activeTile = GetActiveViewportTile();

            if (activeTile == null)
            {
                return;
            }

            if (activeTile.Series == null && SelectedSeries != null)
            {
                AssignSeriesToViewport(activeTile, SelectedSeries);

                return;
            }

            SyncActiveViewportToViewerState(activeTile);

            if (activeTile.CurrentImage == null && !string.IsNullOrWhiteSpace(activeTile.CurrentFilePath))
            {
                TryLoadCurrentImageForViewport(activeTile);
            }
        }

        private void OnKeyDown(KeyEventArgs? e)
        {
            if (e == null)
            {
                return;
            }

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            switch (key)
            {
                case Key.Up:
                case Key.Right:
                case Key.Space:
                    MoveSlice(1);
                    e.Handled = true;
                    break;

                case Key.Down:
                case Key.Left:
                    MoveSlice(-1);
                    e.Handled = true;
                    break;

                case Key.PageUp:
                    MoveSlice(10);
                    e.Handled = true;
                    break;

                case Key.PageDown:
                    MoveSlice(-10);
                    e.Handled = true;
                    break;

                case Key.Home:
                    MoveToFirstSlice();
                    e.Handled = true;
                    break;

                case Key.End:
                    MoveToLastSlice();
                    e.Handled = true;
                    break;
            }
        }

        private void OnMeasurementPoint(ViewportMeasurementPointInfo? pointInfo)
        {
            if (pointInfo == null || string.IsNullOrWhiteSpace(pointInfo.ViewportId))
            {
                return;
            }

            if (ActiveTool != ImagingTool.Measure)
            {
                return;
            }

            if (!_viewportTiles.TryGetValue(pointInfo.ViewportId, out var tile))
            {
                return;
            }

            var frame = tile.CurrentDicomFrame;

            if (frame == null)
            {
                return;
            }

            if (pointInfo.ImageX < 0 || pointInfo.ImageY < 0 || pointInfo.ImageX >= frame.Width || pointInfo.ImageY >= frame.Height)
            {
                return;
            }

            tile.BeginOrCompleteMeasurement(pointInfo.ImageX, pointInfo.ImageY);

            if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal))
            {
                RaisePropertyChanged(nameof(ActiveMeasurements));
                RaisePropertyChanged(nameof(SelectedMeasurement));
                RaisePropertyChanged(nameof(MeasurementDisplayText));
            }
        }

        private void OnMeasurementPreview(ViewportMeasurementPointInfo? pointInfo)
        {
            if (pointInfo == null || string.IsNullOrWhiteSpace(pointInfo.ViewportId))
            {
                return;
            }

            if (ActiveTool != ImagingTool.Measure)
            {
                return;
            }

            if (!_viewportTiles.TryGetValue(pointInfo.ViewportId, out var tile))
            {
                return;
            }

            var frame = tile.CurrentDicomFrame;

            if (frame == null)
            {
                return;
            }

            if (pointInfo.ImageX < 0 || pointInfo.ImageY < 0 || pointInfo.ImageX >= frame.Width || pointInfo.ImageY >= frame.Height)
            {
                return;
            }

            tile.UpdateMeasurementDraft(pointInfo.ImageX, pointInfo.ImageY);

            if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal))
            {
                RaisePropertyChanged(nameof(MeasurementDisplayText));
            }
        }

        private void OnPixelHover(ViewportPixelHoverInfo? hoverInfo)
        {
            if (hoverInfo == null || string.IsNullOrWhiteSpace(hoverInfo.ViewportId))
            {
                return;
            }

            if (!_viewportTiles.TryGetValue(hoverInfo.ViewportId, out var tile))
            {
                return;
            }

            var frame = tile.CurrentDicomFrame;

            if (frame == null || !hoverInfo.HasPixel)
            {
                tile.ClearCurrentPixel();

                if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal))
                {
                    RaisePropertyChanged(nameof(PixelDisplayText));
                }

                return;
            }

            var pixelX = hoverInfo.PixelX!.Value;
            var pixelY = hoverInfo.PixelY!.Value;

            if (pixelX < 0 || pixelY < 0 || pixelX >= frame.Width || pixelY >= frame.Height)
            {
                tile.ClearCurrentPixel();

                if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal))
                {
                    RaisePropertyChanged(nameof(PixelDisplayText));
                }

                return;
            }

            var index = pixelY * frame.Width + pixelX;

            if (index < 0 || index >= frame.Values.Length)
            {
                tile.ClearCurrentPixel();

                if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal))
                {
                    RaisePropertyChanged(nameof(PixelDisplayText));
                }

                return;
            }

            tile.SetCurrentPixel(pixelX, pixelY, frame.Values[index]);

            if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal))
            {
                RaisePropertyChanged(nameof(PixelDisplayText));
            }
        }

        private void OnRenderThrottleTimerTick(object? sender, EventArgs e)
        {
            _renderThrottleTimer.Stop();

            if (_pendingRenderViewportIds.Count == 0)
            {
                return;
            }

            var viewportIds = new List<string>(_pendingRenderViewportIds);

            _pendingRenderViewportIds.Clear();

            foreach (var viewportId in viewportIds)
            {
                if (!_viewportTiles.TryGetValue(viewportId, out var tile))
                {
                    continue;
                }

                RenderCurrentFrameForViewport(tile);
            }
        }

        private void OnSelectedSeriesChanged(object? sender, SeriesSelectionChangedEventArgs e)
        {
            if (_isSynchronizingSelectionFromViewport)
            {
                return;
            }

            ApplySelectedSeries(e.SelectedSeries);
        }

        private async void OnStudyLoaded(object? sender, ImagingStudyLoadedEventArgs e)
        {
            _imageLoadCancellationTokenSource?.Cancel();
            _measurementHydrationCancellationTokenSource?.Cancel();
            _measurementHydrationCancellationTokenSource?.Dispose();
            _measurementHydrationCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _measurementHydrationCancellationTokenSource.Token;

            ClearDicomFrameCache();
            ClearAllViewports(syncActiveViewport: true, clearSelection: false);

            try
            {
                if (e.Study is null)
                {
                    _measurementSessionStore.Clear();
                    return;
                }

                await _measurementHydrationService.HydrateAsync(e.Study.Id, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                ViewportTileViewModel? activeTile = GetActiveViewportTile();
                if (activeTile is not null && e.Series.Count > 0) AssignSeriesToViewport(activeTile, e.Series[0]);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // A newer study-load request or view destruction superseded this hydration operation.
            }
            catch (Exception exception)
            {
                _measurementSessionStore.Clear();
                _wpf.ReportException(exception);
            }
        }

        private void OnViewportStateChanged(object? sender, ImagingViewportStateChangedEventArgs e) => ApplyViewportState(e.State);

        private void OnViewportTransformCommandRequested(ViewportTransformCommand command)
        {
            var activeTile = GetActiveViewportTile();
            if (activeTile == null) return;

            switch (command)
            {
                case ViewportTransformCommand.RotateLeft: activeTile.RotateLeft(); break;
                case ViewportTransformCommand.RotateRight: activeTile.RotateRight(); break;
                case ViewportTransformCommand.ResetRotation: activeTile.ResetRotation(); break;
                case ViewportTransformCommand.FlipHorizontal: activeTile.FlipHorizontal(); break;
                case ViewportTransformCommand.FlipVertical: activeTile.FlipVertical(); break;
                case ViewportTransformCommand.ResetFlip: activeTile.ResetFlip(); break;
                case ViewportTransformCommand.ResetViewTransform: activeTile.ResetViewTransform(); break;
            }
        }

        private void OnWindowLevelChanged(object? sender, ImagingWindowLevelChangedEventArgs e)
        {
            var activeTile = GetActiveViewportTile();

            if (activeTile == null)
            {
                return;
            }

            if (e.IsReset)
            {
                activeTile.ResetWindowLevel();
            }
            else
            {
                activeTile.SetWindowLevel(e.WindowCenter, e.WindowWidth);
            }

            RaisePropertyChanged(nameof(WindowDisplayText));

            if (activeTile.CurrentDicomFrame != null)
            {
                RequestRenderCurrentFrameForViewport(activeTile);
                return;
            }

            TryLoadCurrentImageForViewport(activeTile);
        }

        private void OnWindowLevelDrag(ViewportWindowLevelDragInfo? dragInfo)
        {
            if (dragInfo == null)
            {
                return;
            }

            if (ActiveTool != ImagingTool.WindowLevel)
            {
                return;
            }

            if (!string.Equals(_windowLevelDragViewportId, dragInfo.ViewportId, StringComparison.Ordinal))
            {
                return;
            }

            if (!_viewportTiles.TryGetValue(dragInfo.ViewportId, out var tile))
            {
                return;
            }

            if (tile.CurrentDicomFrame == null)
            {
                return;
            }

            var newWidth = _windowLevelDragStartWidth + dragInfo.TotalDeltaX * 2.0;
            var newCenter = _windowLevelDragStartCenter - dragInfo.TotalDeltaY * 1.0;

            if (newWidth < 1)
            {
                newWidth = 1;
            }

            tile.SetWindowLevel(newCenter, newWidth);

            RaisePropertyChanged(nameof(WindowDisplayText));

            RequestRenderCurrentFrameForViewport(tile);
        }

        private void OnWindowLevelDragCompleted(string? viewportId)
        {
            if (string.Equals(_windowLevelDragViewportId, viewportId, StringComparison.Ordinal))
            {
                _windowLevelDragViewportId = null;
            }
        }

        private void OnWindowLevelDragStarted(string? viewportId)
        {
            _windowLevelDragViewportId = null;

            if (string.IsNullOrWhiteSpace(viewportId))
            {
                return;
            }

            if (ActiveTool != ImagingTool.WindowLevel)
            {
                return;
            }

            if (!_viewportTiles.TryGetValue(viewportId, out var tile))
            {
                return;
            }

            if (tile.CurrentDicomFrame == null)
            {
                return;
            }

            var frame = tile.CurrentDicomFrame;

            _windowLevelDragViewportId = viewportId;

            _windowLevelDragStartCenter = tile.WindowCenter ?? frame.DefaultWindowCenter ?? 40.0;

            _windowLevelDragStartWidth = tile.WindowWidth ?? frame.DefaultWindowWidth ?? 400.0;
        }

        private ViewportTileViewModel RegisterViewport(string viewportId, string title)
        {
            var tile = new ViewportTileViewModel(viewportId, title, _measurementSessionStore, _measurementInstanceResolver);
            tile.SetActiveTool(ActiveTool);
            tile.PropertyChanged += OnViewportTilePropertyChanged;
            tile.SliceNavigationRequested += OnSliceNavigationRequested;
            tile.ViewportStateChanged += OnViewportStateChangeRequested;

            _viewportTiles[viewportId] = tile;

            return tile;
        }

        private bool RenderCurrentFrameForViewport(ViewportTileViewModel tile)
        {
            var frame = tile.CurrentDicomFrame;

            if (frame == null)
            {
                return false;
            }

            var dicomImage = _dicom.ImageService!.RenderGrayscaleImage(frame, tile.WindowCenter, tile.WindowWidth);

            if (dicomImage == null)
            {
                tile.CurrentImage = null;

                if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal))
                {
                    SyncActiveViewportToViewerState(tile);
                }

                return false;
            }

            var imageSource = _wpf.MediaService?.ImageSourceService?.CreateImageSource(dicomImage);

            tile.CurrentImage = imageSource;

            if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal))
            {
                SyncActiveViewportToViewerState(tile);
            }

            return imageSource != null;
        }

        private void RequestRenderCurrentFrameForViewport(ViewportTileViewModel tile)
        {
            if (tile.CurrentDicomFrame == null)
            {
                return;
            }

            _pendingRenderViewportIds.Add(tile.ViewportId);

            if (!_renderThrottleTimer.IsEnabled)
            {
                _renderThrottleTimer.Start();
            }
        }

        private void SelectViewport(string? viewportId)
        {
            if (string.IsNullOrWhiteSpace(viewportId))
            {
                return;
            }

            _core.ImagingService!.ImagingViewportSelectionService!.SelectViewport(viewportId);
        }

        private void SyncActiveViewportToViewerState(ViewportTileViewModel tile)
        {
            SelectedSeries = tile.Series;
            ActiveSeriesFiles = tile.SeriesFiles;

            _currentSlice = tile.CurrentSlice;
            RaisePropertyChanged(nameof(CurrentSlice));
            RaisePropertyChanged(nameof(SliceDisplayText));

            SliceCount = tile.SliceCount;

            CurrentFileName = tile.CurrentFileName;
            CurrentFilePath = tile.CurrentFilePath;
            CurrentImage = tile.CurrentImage;
            ZoomFactor = tile.ZoomFactor;

            RaisePropertyChanged(nameof(WindowDisplayText));
            RaisePropertyChanged(nameof(PixelDisplayText));
            RaisePropertyChanged(nameof(MeasurementDisplayText));

            _measurementContext.SetActiveViewport(tile);

            _core.ImagingService!.ImagingViewportService!.SetSlice(tile.CurrentSlice, tile.SliceCount);
        }

        private void TrimDicomFrameCache()
        {
            while (_dicomFrameCache.Count > MaxCachedDicomFrames && _dicomFrameCacheOrder.Count > 0)
            {
                var oldestFilePath = _dicomFrameCacheOrder.Dequeue();

                _dicomFrameCache.Remove(oldestFilePath);
            }
        }

        private async void TryLoadCurrentImageForViewport(ViewportTileViewModel tile)
        {
            var filePath = tile.CurrentFilePath;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                tile.CurrentDicomFrame = null;
                tile.CurrentImage = null;

                if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal)) SyncActiveViewportToViewerState(tile);
                return;
            }

            var requestedFilePath = filePath;

            _imageLoadCancellationTokenSource?.Cancel();
            _imageLoadCancellationTokenSource?.Dispose();
            _imageLoadCancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = _imageLoadCancellationTokenSource.Token;

            try
            {
                // Every displayed DICOM slice requires its frame for rendering, hover, measurements and the canonical viewport geometry.
                var frame = await GetOrLoadDicomFrameAsync(requestedFilePath, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.Equals(tile.CurrentFilePath, requestedFilePath, StringComparison.OrdinalIgnoreCase)) return;

                tile.CurrentDicomFrame = frame;

                if (frame == null)
                {
                    tile.CurrentImage = null;
                }
                else
                {
                    var dicomImage = _dicom.ImageService!.RenderGrayscaleImage(frame, tile.WindowCenter, tile.WindowWidth);
                    tile.CurrentImage = dicomImage == null ? null : _wpf.MediaService?.ImageSourceService?.CreateImageSource(dicomImage);
                }

                if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal)) SyncActiveViewportToViewerState(tile);
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                if (!string.Equals(tile.CurrentFilePath, requestedFilePath, StringComparison.OrdinalIgnoreCase)) return;

                tile.CurrentDicomFrame = null;
                tile.CurrentImage = null;

                if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal)) SyncActiveViewportToViewerState(tile);
            }
        }
    }
}