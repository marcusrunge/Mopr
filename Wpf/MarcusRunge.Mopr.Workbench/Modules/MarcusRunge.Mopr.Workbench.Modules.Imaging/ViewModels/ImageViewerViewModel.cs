using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
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
        private string? _currentFileName, _currentFilePath;
        private ImageSource? _currentImage;
        private ImagingLayout _currentLayout;
        private int _currentSlice = 1, _sliceCount = 1;
        private CancellationTokenSource? _imageLoadCancellationTokenSource;
        private bool _isSynchronizingSelectionFromViewport;
        private SeriesInfo? _selectedSeries;
        private double _zoomFactor = 1.0;

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

            _core.ImagingService!.ImagingSelectionService!.SelectedSeriesChanged += OnSelectedSeriesChanged;
            _core.ImagingService!.ImagingToolService!.ActiveToolChanged += OnActiveToolChanged;
            _core.ImagingService!.ImagingViewportService!.StateChanged += OnViewportStateChanged;
            _core.ImagingService!.ImagingLayoutService!.CurrentLayoutChanged += OnCurrentLayoutChanged;
            _core.ImagingService!.ImagingViewportSelectionService!.ActiveViewportChanged += OnActiveViewportChanged;
            _core.ImagingService!.ImagingWindowLevelService!.WindowLevelChanged += OnWindowLevelChanged;
            _core.ImagingService!.ImagingStudyService!.StudyLoaded += OnStudyLoaded;
            _activeTool = _core.ImagingService!.ImagingToolService!.ActiveTool;

            SelectViewportCommand = new DelegateCommand<string?>(SelectViewport);
            MouseWheelCommand = new DelegateCommand<MouseWheelEventArgs?>(OnMouseWheel);
            KeyDownCommand = new DelegateCommand<KeyEventArgs?>(OnKeyDown);
            ClearViewportCommand = new DelegateCommand<string?>(ClearViewport);

            SingleMainViewport = RegisterViewport("Single.Main", "Single");

            TwoByTwoViewport1 = RegisterViewport("TwoByTwo.Viewport1", "Viewport 1");
            TwoByTwoViewport2 = RegisterViewport("TwoByTwo.Viewport2", "Viewport 2");
            TwoByTwoViewport3 = RegisterViewport("TwoByTwo.Viewport3", "Viewport 3");
            TwoByTwoViewport4 = RegisterViewport("TwoByTwo.Viewport4", "Viewport 4");

            MprAxialViewport = RegisterViewport("Mpr.Axial", "Axial");
            MprSagittalViewport = RegisterViewport("Mpr.Sagittal", "Sagittal");
            MprCoronalViewport = RegisterViewport("Mpr.Coronal", "Coronal");
            MprPreview3DViewport = RegisterViewport("Mpr.Preview3D", "3D / Preview");

            AscAxialViewport = RegisterViewport("Asc.Axial", "Axial");
            AscSagittalViewport = RegisterViewport("Asc.Sagittal", "Sagittal");
            AscCoronalViewport = RegisterViewport("Asc.Coronal", "Coronal");

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
                }
            }
        }

        public string ActiveToolDisplayText => $"Werkzeug: {ActiveTool}";

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

                return activeViewport == null ? "Viewport: -" : $"Viewport: {activeViewport.Title}";
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

        public string CurrentFileDisplayText => string.IsNullOrWhiteSpace(CurrentFileName) ? "Datei: -" : $"Datei: {CurrentFileName}";

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

        public string CurrentFileToolTipText => string.IsNullOrWhiteSpace(CurrentFilePath) ? "Keine Datei" : $"Aktuelle Datei:{Environment.NewLine}{CurrentFilePath}";

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

        public bool IsMprLayoutVisible => CurrentLayout == ImagingLayout.Mpr;

        public bool IsSingleLayoutVisible => CurrentLayout == ImagingLayout.Single;

        public bool IsTwoByTwoLayoutVisible => CurrentLayout == ImagingLayout.TwoByTwo;

        public DelegateCommand<KeyEventArgs?> KeyDownCommand { get; }

        public string LayoutDisplayText => CurrentLayout switch
        {
            ImagingLayout.Single => "Layout: Einzelansicht",
            ImagingLayout.TwoByTwo => "Layout: 2 × 2",
            ImagingLayout.Mpr => "Layout: MPR",
            ImagingLayout.AxialSagittalCoronal => "Layout: Axial / Sagittal / Coronal",
            _ => "Layout: Unbekannt"
        };

        public DelegateCommand<MouseWheelEventArgs?> MouseWheelCommand { get; }

        public ViewportTileViewModel MprAxialViewport { get; }

        public ViewportTileViewModel MprCoronalViewport { get; }

        public ViewportTileViewModel MprPreview3DViewport { get; }

        public ViewportTileViewModel MprSagittalViewport { get; }

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

        public string ViewerSubtitle => SelectedSeries == null ? "Keine Serie aktiv" : $"{SelectedSeries.Modality} · {SelectedSeries.Description}";

        public string ViewerTitle => SelectedSeries == null ? "Image Viewer" : SelectedSeries.Name;

        public string WindowDisplayText => GetActiveViewportTile()?.WindowDisplayText ?? "W/L: Auto";
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