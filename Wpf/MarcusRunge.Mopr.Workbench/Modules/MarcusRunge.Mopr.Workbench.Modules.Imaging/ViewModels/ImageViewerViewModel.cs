using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ImageViewerViewModel : ViewModelBase
    {
        private readonly ICore _core;
        private readonly IWpf _wpf;

        private IReadOnlyList<string> _activeSeriesFiles = [];
        private ImagingTool _activeTool;
        private string _activeViewportId = "Single.Main";
        private string? _currentFileName, _currentFilePath, _loadedImagePath;
        private ImageSource? _currentImage;
        private ImagingLayout _currentLayout;
        private int _currentSlice = 1, _sliceCount = 1;
        private SeriesInfo? _selectedSeries;
        private double _zoomFactor = 1.0;

        public ImageViewerViewModel(ICore core, IWpf wpf)
        {
            _core = core;
            _wpf = wpf;

            _currentLayout = _core.ImagingService!.ImagingLayoutService!.CurrentLayout;

            _core.ImagingService!.ImagingViewportSelectionService!.SetDefaultViewport(
                GetDefaultViewportIdForLayout(_currentLayout));

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

                    if (string.Equals(
                            viewport.Id,
                            ActiveViewportId,
                            StringComparison.Ordinal))
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
                if (SetProperty(ref _currentSlice, value))
                {
                    RaisePropertyChanged(nameof(SliceDisplayText));

                    _core.ImagingService!.ImagingViewportService!.SetSlice(value, SliceCount);
                }
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

            base.Destroy();
        }

        private void ApplySelectedSeries(SeriesInfo? series)
        {
            SelectedSeries = series;

            CurrentImage = null;
            CurrentFileName = null;
            CurrentFilePath = null;
            _loadedImagePath = null;
            ActiveSeriesFiles = [];

            if (series == null)
            {
                _core.ImagingService!.ImagingViewportService!.SetSlice(1, 1);

                return;
            }

            var files = _core.ImagingService!.ImagingStudyService!.GetFilesForSeries(series.Id);

            ActiveSeriesFiles = files;

            var sliceCount = files.Count > 0 ? files.Count : Math.Max(1, series.ImageCount);

            _core.ImagingService!.ImagingViewportService!.SetSlice(1, sliceCount);

            UpdateCurrentFileName(1);
        }

        private void ApplyViewportState(ImagingViewportState state)
        {
            SliceCount = state.SliceCount;

            if (_currentSlice != state.CurrentSlice)
            {
                _currentSlice = state.CurrentSlice;
                RaisePropertyChanged(nameof(CurrentSlice));
                RaisePropertyChanged(nameof(SliceDisplayText));
            }

            ZoomFactor = state.ZoomFactor;

            UpdateCurrentFileName(state.CurrentSlice);
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

        private void MoveSlice(int delta) => _core.ImagingService!.ImagingViewportService!.MoveSlice(delta);

        private void MoveToFirstSlice() => _core.ImagingService!.ImagingViewportService!.SetSlice(1, SliceCount);

        private void MoveToLastSlice() => _core.ImagingService!.ImagingViewportService!.SetSlice(SliceCount, SliceCount);

        private void OnActiveToolChanged(object? sender, ImagingToolChangedEventArgs e) => ActiveTool = e.NewTool;

        private void OnActiveViewportChanged(
            object? sender,
            ImagingViewportSelectionChangedEventArgs e) => ActiveViewportId = e.NewViewportId;

        private void OnCurrentLayoutChanged(
            object? sender,
            ImagingLayoutChangedEventArgs e)
        {
            CurrentLayout = e.NewLayout;

            _core.ImagingService!.ImagingViewportSelectionService!.SetDefaultViewport(GetDefaultViewportIdForLayout(e.NewLayout));
        }

        private void OnKeyDown(KeyEventArgs? e)
        {
            if (e == null)
            {
                return;
            }

            var key = e.Key == Key.System
                ? e.SystemKey
                : e.Key;

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

        private void OnSelectedSeriesChanged(object? sender, SeriesSelectionChangedEventArgs e) => ApplySelectedSeries(e.SelectedSeries);

        private void OnViewportStateChanged(object? sender, ImagingViewportStateChangedEventArgs e) => ApplyViewportState(e.State);

        private void SelectViewport(string? viewportId)
        {
            if (string.IsNullOrWhiteSpace(viewportId))
            {
                return;
            }

            _core.ImagingService!.ImagingViewportSelectionService!.SelectViewport(viewportId);
        }

        private void TryLoadCurrentImage(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                CurrentImage = null;
                _loadedImagePath = null;
                return;
            }

            if (string.Equals(
                    _loadedImagePath,
                    filePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var imageSource = _wpf.MediaService?.ImageSourceService?.LoadImageSource(filePath);

            CurrentImage = imageSource;
            _loadedImagePath = imageSource == null
                ? null
                : filePath;
        }

        private void UpdateCurrentFileName(int currentSlice)
        {
            if (ActiveSeriesFiles.Count == 0)
            {
                CurrentFileName = null;
                CurrentFilePath = null;
                CurrentImage = null;
                _loadedImagePath = null;
                return;
            }

            var index = currentSlice - 1;

            if (index < 0 || index >= ActiveSeriesFiles.Count)
            {
                CurrentFileName = null;
                CurrentFilePath = null;
                CurrentImage = null;
                _loadedImagePath = null;
                return;
            }

            var filePath = ActiveSeriesFiles[index];

            CurrentFilePath = filePath;
            CurrentFileName = Path.GetFileName(filePath);

            TryLoadCurrentImage(filePath);
        }
    }
}