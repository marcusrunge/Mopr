using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
using Prism.Commands;
using System;
using System.Collections.Generic;
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
        private readonly Dictionary<string, DicomImageFrame> _dicomFrameCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<string> _dicomFrameCacheOrder = new();
        private readonly HashSet<string> _pendingRenderViewportIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly System.Windows.Threading.DispatcherTimer _renderThrottleTimer = new System.Windows.Threading.DispatcherTimer();
        private readonly Dictionary<string, ViewportTileViewModel> _viewportTiles = new(StringComparer.OrdinalIgnoreCase);
        private readonly IWpf _wpf;
        private IReadOnlyList<string> _activeSeriesFiles = [];
        private ImagingTool _activeTool;
        private string _activeViewportId = "Single.Main";
        private string? _currentFileName, _currentFilePath, _windowLevelDragViewportId;
        private ImageSource? _currentImage;
        private ImagingLayout _currentLayout;
        private int _currentSlice = 1, _sliceCount = 1;
        private CancellationTokenSource? _imageLoadCancellationTokenSource;
        private bool _isSynchronizingSelectionFromViewport;
        private SeriesInfo? _selectedSeries;
        private double _zoomFactor = 1.0, _windowLevelDragStartCenter, _windowLevelDragStartWidth;

        public ImageViewerViewModel(ICore core, IWpf wpf, IDicom dicom)
        {
            _core = core;
            _wpf = wpf;
            _dicom = dicom;

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
            _activeTool = _core.ImagingService!.ImagingToolService!.ActiveTool;

            ClearViewportCommand = new DelegateCommand<string?>(ClearViewport);
            KeyDownCommand = new DelegateCommand<KeyEventArgs?>(OnKeyDown);
            MeasurementPointCommand = new DelegateCommand<ViewportPixelHoverInfo?>(OnMeasurementPoint);
            MouseWheelCommand = new DelegateCommand<MouseWheelEventArgs?>(OnMouseWheel);
            PixelHoverCommand = new DelegateCommand<ViewportPixelHoverInfo?>(OnPixelHover);
            SelectViewportCommand = new DelegateCommand<string?>(SelectViewport);
            WindowLevelDragCommand = new DelegateCommand<ViewportWindowLevelDragInfo?>(OnWindowLevelDrag);
            WindowLevelDragCompletedCommand = new DelegateCommand<string?>(OnWindowLevelDragCompleted);
            WindowLevelDragStartedCommand = new DelegateCommand<string?>(OnWindowLevelDragStarted);

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

        public DelegateCommand<string?> ClearViewportCommand { get; }

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
                    RaisePropertyChanged(nameof(LayoutDisplayText));
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

        public DelegateCommand<KeyEventArgs?> KeyDownCommand { get; }

        public string LayoutDisplayText => CurrentLayout switch
        {
            ImagingLayout.Single => Resources.Viewer_Layout_Single,
            ImagingLayout.TwoByTwo => Resources.Viewer_Layout_TwoByTwo,
            ImagingLayout.Mpr => Resources.Viewer_Layout_Mpr,
            ImagingLayout.AxialSagittalCoronal => Resources.Viewer_Layout_AxialSagittalCoronal,
            _ => Resources.Viewer_Layout_Unknown
        };

        public string MeasurementDisplayText => GetActiveViewportTile()?.MeasurementDisplayText ?? Resources.Status_MeasurementEmpty;

        public DelegateCommand<ViewportPixelHoverInfo?> MeasurementPointCommand { get; }

        public DelegateCommand<MouseWheelEventArgs?> MouseWheelCommand { get; }

        public ViewportTileViewModel MprAxialViewport { get; }

        public ViewportTileViewModel MprCoronalViewport { get; }

        public ViewportTileViewModel MprPreview3DViewport { get; }

        public ViewportTileViewModel MprSagittalViewport { get; }

        public string PixelDisplayText => GetActiveViewportTile()?.CurrentPixelDisplayText ?? $"{Resources.Viewer_Pixel}: -";

        public DelegateCommand<ViewportPixelHoverInfo?> PixelHoverCommand { get; }

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

        public DelegateCommand<string?> SelectViewportCommand { get; }

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

        public string ViewerSubtitle => SelectedSeries == null ? Resources.Viewer_NoSeries : $"{SelectedSeries.Modality} · {SelectedSeries.Description}";

        public string ViewerTitle => SelectedSeries == null ? Resources.Viewer_Title : SelectedSeries.Name;

        public string WindowDisplayText => GetActiveViewportTile()?.WindowDisplayText ?? $"{Resources.Viewer_WindowLevel}: {Resources.Viewer_Default}";

        public DelegateCommand<ViewportWindowLevelDragInfo?> WindowLevelDragCommand { get; }

        public DelegateCommand<string?> WindowLevelDragCompletedCommand { get; }

        public DelegateCommand<string?> WindowLevelDragStartedCommand { get; }

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

            _imageLoadCancellationTokenSource?.Cancel();
            _imageLoadCancellationTokenSource?.Dispose();
            _imageLoadCancellationTokenSource = null;

            ClearDicomFrameCache();

            _renderThrottleTimer.Stop();
            _renderThrottleTimer.Tick -= OnRenderThrottleTimerTick;
            _pendingRenderViewportIds.Clear();

            base.Destroy();
        }

        private static string GetToolDisplayText(ImagingTool tool)
        {
            return tool switch
            {
                ImagingTool.WindowLevel => Resources.Viewer_Tool_WindowLevel,
                ImagingTool.Zoom => Resources.Viewer_Tool_Zoom,
                ImagingTool.Pan => Resources.Viewer_Tool_Pan,
                ImagingTool.Crosshair => Resources.Viewer_Tool_Crosshair,
                ImagingTool.Measure => Resources.Viewer_Tool_Measure,
                _ => Resources.Viewer_Tool_None
            };
        }

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

            if (activeTile == null)
            {
                return;
            }

            activeTile.MoveSlice(delta);

            SyncActiveViewportToViewerState(activeTile);

            TryLoadCurrentImageForViewport(activeTile);
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

        private void OnActiveToolChanged(object? sender, ImagingToolChangedEventArgs e) => ActiveTool = e.NewTool;

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

        private void OnMeasurementPoint(ViewportPixelHoverInfo? hoverInfo)
        {
            if (hoverInfo == null || !hoverInfo.HasPixel || string.IsNullOrWhiteSpace(hoverInfo.ViewportId))
            {
                return;
            }

            if (ActiveTool != ImagingTool.Measure)
            {
                return;
            }

            if (!_viewportTiles.TryGetValue(hoverInfo.ViewportId, out var tile))
            {
                return;
            }

            var frame = tile.CurrentDicomFrame;

            if (frame == null)
            {
                return;
            }

            var pixelX = hoverInfo.PixelX!.Value;
            var pixelY = hoverInfo.PixelY!.Value;

            if (pixelX < 0 || pixelY < 0 || pixelX >= frame.Width || pixelY >= frame.Height)
            {
                return;
            }

            tile.AddMeasurementPoint(
                pixelX,
                pixelY);

            if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal))
            {
                RaisePropertyChanged(nameof(MeasurementDisplayText));
            }
        }

        private void OnMouseWheel(MouseWheelEventArgs? e)
        {
            if (e == null)
            {
                return;
            }

            var step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;

            if (e.Delta > 0)
            {
                MoveSlice(step);
            }
            else if (e.Delta < 0)
            {
                MoveSlice(-step);
            }

            e.Handled = true;
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

        private void OnStudyLoaded(object? sender, ImagingStudyLoadedEventArgs e)
        {
            _imageLoadCancellationTokenSource?.Cancel();

            ClearDicomFrameCache();

            ClearAllViewports(syncActiveViewport: true, clearSelection: false);

            var activeTile = GetActiveViewportTile();

            if (activeTile == null)
            {
                return;
            }

            if (e.Series.Count > 0)
            {
                AssignSeriesToViewport(activeTile, e.Series[0]);
            }
        }

        private void OnViewportStateChanged(object? sender, ImagingViewportStateChangedEventArgs e) => ApplyViewportState(e.State);

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
            var tile = new ViewportTileViewModel(viewportId, title);

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

            RaisePropertyChanged(nameof(WindowDisplayText));
            RaisePropertyChanged(nameof(PixelDisplayText));
            RaisePropertyChanged(nameof(MeasurementDisplayText));

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

                if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal))
                {
                    SyncActiveViewportToViewerState(tile);
                }

                return;
            }

            var requestedFilePath = filePath;

            _imageLoadCancellationTokenSource?.Cancel();
            _imageLoadCancellationTokenSource?.Dispose();
            _imageLoadCancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = _imageLoadCancellationTokenSource.Token;

            try
            {
                var imageSource = _wpf.MediaService?.ImageSourceService?.LoadImageSource(requestedFilePath);

                if (imageSource != null)
                {
                    tile.CurrentDicomFrame = null;
                }
                else
                {
                    var frame = tile.CurrentDicomFrame;

                    if (frame == null || !string.Equals(frame.FilePath, requestedFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        frame = await GetOrLoadDicomFrameAsync(requestedFilePath, cancellationToken);

                        cancellationToken.ThrowIfCancellationRequested();

                        if (!string.Equals(tile.CurrentFilePath, requestedFilePath, StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }

                        tile.CurrentDicomFrame = frame;
                    }

                    if (frame != null)
                    {
                        var dicomImage = _dicom.ImageService!.RenderGrayscaleImage(frame, tile.WindowCenter, tile.WindowWidth);

                        if (dicomImage != null)
                        {
                            imageSource = _wpf.MediaService?.ImageSourceService?.CreateImageSource(dicomImage);
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (!string.Equals(tile.CurrentFilePath, requestedFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                tile.CurrentImage = imageSource;

                if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal))
                {
                    SyncActiveViewportToViewerState(tile);
                }
            }
            catch (OperationCanceledException)
            {
                // Ein neuer Slice, Viewport oder Ladevorgang hat diesen Auftrag abgebrochen.
            }
            catch
            {
                tile.CurrentImage = null;

                if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal))
                {
                    SyncActiveViewportToViewerState(tile);
                }
            }
        }
    }
}