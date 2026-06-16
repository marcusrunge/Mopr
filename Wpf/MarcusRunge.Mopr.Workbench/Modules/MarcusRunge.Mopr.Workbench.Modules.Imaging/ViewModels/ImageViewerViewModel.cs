using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging;
using Prism.Commands;
using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ImageViewerViewModel : ViewModelBase
    {
        private readonly IImagingSelectionService _selectionService;
        private readonly IImagingToolService _toolService;
        private readonly IImagingViewportService _viewportService;

        private ImagingTool _activeTool;
        private ImageSource? _currentImage;
        private int _currentSlice = 1;
        private SeriesInfo? _selectedSeries;
        private int _sliceCount = 1;
        private double _zoomFactor = 1.0;

        public ImageViewerViewModel(IImagingSelectionService selectionService, IImagingToolService toolService, IImagingViewportService viewportService)
        {
            _selectionService = selectionService;
            _toolService = toolService;
            _viewportService = viewportService;

            _selectionService.SelectedSeriesChanged += OnSelectedSeriesChanged;
            _toolService.ActiveToolChanged += OnActiveToolChanged;
            _viewportService.StateChanged += OnViewportStateChanged;

            _activeTool = _toolService.ActiveTool;

            ZoomCommand = new DelegateCommand(ActivateZoom);
            PanCommand = new DelegateCommand(ActivatePan);
            WindowLevelCommand = new DelegateCommand(ActivateWindowLevel);
            CrosshairCommand = new DelegateCommand(ActivateCrosshair);
            ResetViewCommand = new DelegateCommand(ResetView);

            ApplyViewportState(_viewportService.State);
            ApplySelectedSeries(_selectionService.SelectedSeries);
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
        public DelegateCommand CrosshairCommand { get; }

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

        public int CurrentSlice
        {
            get => _currentSlice;
            set
            {
                if (SetProperty(ref _currentSlice, value))
                {
                    RaisePropertyChanged(nameof(SliceDisplayText));
                    _viewportService.SetSlice(value, SliceCount);
                }
            }
        }

        public bool IsEmptyViewerVisible => CurrentImage == null;
        public DelegateCommand PanCommand { get; }
        public DelegateCommand ResetViewCommand { get; }

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

        public DelegateCommand WindowLevelCommand { get; }
        public DelegateCommand ZoomCommand { get; }
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
            _selectionService.SelectedSeriesChanged -= OnSelectedSeriesChanged;
            _toolService.ActiveToolChanged -= OnActiveToolChanged;
            _viewportService.StateChanged -= OnViewportStateChanged;

            base.Destroy();
        }

        private void ActivateCrosshair() => _toolService.SetActiveTool(ImagingTool.Crosshair);

        private void ActivatePan() => _toolService.SetActiveTool(ImagingTool.Pan);

        private void ActivateWindowLevel() => _toolService.SetActiveTool(ImagingTool.WindowLevel);

        private void ActivateZoom() => _toolService.SetActiveTool(ImagingTool.Zoom);

        private void ApplySelectedSeries(SeriesInfo? series)
        {
            SelectedSeries = series;

            if (series == null)
            {
                CurrentImage = null;
                _viewportService.SetSlice(1, 1);
                return;
            }

            _viewportService.SetSlice(1, series.ImageCount);

            CurrentImage = null;
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
        }

        private void OnActiveToolChanged(object? sender, ImagingToolChangedEventArgs e) => ActiveTool = e.NewTool;

        private void OnSelectedSeriesChanged(object? sender, SeriesSelectionChangedEventArgs e) => ApplySelectedSeries(e.SelectedSeries);

        private void OnViewportStateChanged(object? sender, ImagingViewportStateChangedEventArgs e) => ApplyViewportState(e.State);

        private void ResetView()
        {
            _viewportService.Reset();
            _toolService.ClearActiveTool();
        }
    }
}