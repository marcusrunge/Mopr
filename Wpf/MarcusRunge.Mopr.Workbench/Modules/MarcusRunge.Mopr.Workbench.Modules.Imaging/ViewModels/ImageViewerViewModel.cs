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
using System.Windows.Input;
using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ImageViewerViewModel : ViewModelBase
    {
        private readonly ICore _core;
        private readonly IDicom _dicom;
        private readonly Dictionary<string, ViewportTileViewModel> _viewportTiles = new(StringComparer.OrdinalIgnoreCase);
        private readonly IWpf _wpf;
        private IReadOnlyList<string> _activeSeriesFiles = [];

        private ImagingTool _activeTool;
        private string _activeViewportId = "Single.Main";
        private string? _currentFileName;
        private string? _currentFilePath;
        private ImageSource? _currentImage;
        private ImagingLayout _currentLayout;
        private int _currentSlice = 1;
        private CancellationTokenSource? _imageLoadCancellationTokenSource;
        private SeriesInfo? _selectedSeries;
        private int _sliceCount = 1;
        private double _zoomFactor = 1.0;

        public ImageViewerViewModel(ICore core, IWpf wpf, IDicom dicom)
        {
            _core = core;
            _wpf = wpf;
            _dicom = dicom;

            _currentLayout = _core.ImagingService!.ImagingLayoutService!.CurrentLayout;

            _core.ImagingService!.ImagingViewportSelectionService!.SetDefaultViewport(GetDefaultViewportIdForLayout(_currentLayout));

            _activeViewportId = _core.ImagingService!.ImagingViewportSelectionService!.ActiveViewportId;

            _core.ImagingService!.ImagingSelectionService!.SelectedSeriesChanged += OnSelectedSeriesChanged;
            _core.ImagingService!.ImagingToolService!.ActiveToolChanged += OnActiveToolChanged;
            _core.ImagingService!.ImagingViewportService!.StateChanged += OnViewportStateChanged;
            _core.ImagingService!.ImagingLayoutService!.CurrentLayoutChanged += OnCurrentLayoutChanged;
            _core.ImagingService!.ImagingViewportSelectionService!.ActiveViewportChanged += OnActiveViewportChanged;

            _activeTool = _core.ImagingService!.ImagingToolService!.ActiveTool;

            SelectViewportCommand = new DelegateCommand<string?>(SelectViewport);
            MouseWheelCommand = new DelegateCommand<MouseWheelEventArgs?>(OnMouseWheel);
            KeyDownCommand = new DelegateCommand<KeyEventArgs?>(OnKeyDown);

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

            _imageLoadCancellationTokenSource?.Cancel();
            _imageLoadCancellationTokenSource?.Dispose();
            _imageLoadCancellationTokenSource = null;

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

        private ViewportTileViewModel? GetActiveViewportTile()
        {
            return _viewportTiles.TryGetValue(ActiveViewportId, out var tile) ? tile : null;
        }

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

        private void OnActiveToolChanged(object? sender, ImagingToolChangedEventArgs e)
        {
            ActiveTool = e.NewTool;
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

        private void OnSelectedSeriesChanged(object? sender, SeriesSelectionChangedEventArgs e)
        {
            ApplySelectedSeries(e.SelectedSeries);
        }

        private void OnViewportStateChanged(object? sender, ImagingViewportStateChangedEventArgs e)
        {
            ApplyViewportState(e.State);
        }

        private ViewportTileViewModel RegisterViewport(string viewportId, string title)
        {
            var tile = new ViewportTileViewModel(viewportId, title);

            _viewportTiles[viewportId] = tile;

            return tile;
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

            _core.ImagingService!.ImagingViewportService!.SetSlice(tile.CurrentSlice, tile.SliceCount);
        }

        private async void TryLoadCurrentImageForViewport(ViewportTileViewModel tile)
        {
            var filePath = tile.CurrentFilePath;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                tile.CurrentImage = null;

                if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal))
                {
                    SyncActiveViewportToViewerState(tile);
                }

                return;
            }

            _imageLoadCancellationTokenSource?.Cancel();
            _imageLoadCancellationTokenSource?.Dispose();
            _imageLoadCancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = _imageLoadCancellationTokenSource.Token;

            try
            {
                var imageSource = _wpf.MediaService?.ImageSourceService?.LoadImageSource(filePath);

                if (imageSource == null)
                {
                    var dicomImage = await _dicom.ImageService!.LoadGrayscaleImageAsync(filePath, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    if (dicomImage != null)
                    {
                        imageSource = _wpf.MediaService?.ImageSourceService?.CreateImageSource(dicomImage);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                tile.CurrentImage = imageSource;

                if (string.Equals(tile.ViewportId, ActiveViewportId, StringComparison.Ordinal))
                {
                    SyncActiveViewportToViewerState(tile);
                }
            }
            catch (OperationCanceledException)
            {
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