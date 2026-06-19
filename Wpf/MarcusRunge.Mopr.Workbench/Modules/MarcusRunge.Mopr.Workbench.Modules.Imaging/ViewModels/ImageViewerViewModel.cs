using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging;
using Prism.Commands;
using System;
using System.Linq;
using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ImageViewerViewModel : ViewModelBase
    {
        private readonly IImagingLayoutService _layoutService;
        private readonly IImagingSelectionService _selectionService;
        private readonly IImagingToolService _toolService;
        private readonly IImagingViewportSelectionService _viewportSelectionService;
        private readonly IImagingViewportService _viewportService;
        private ImagingTool _activeTool;
        private string _activeViewportId = "Single.Main";
        private ImageSource? _currentImage;
        private ImagingLayout _currentLayout;
        private int _currentSlice = 1;
        private SeriesInfo? _selectedSeries;
        private int _sliceCount = 1;
        private double _zoomFactor = 1.0;

        public ImageViewerViewModel(IImagingSelectionService selectionService, IImagingToolService toolService, IImagingViewportService viewportService, IImagingLayoutService layoutService, IImagingViewportSelectionService viewportSelectionService)
        {
            _selectionService = selectionService;
            _toolService = toolService;
            _viewportService = viewportService;
            _layoutService = layoutService;
            _viewportSelectionService = viewportSelectionService;

            _currentLayout = _layoutService.CurrentLayout;

            _viewportSelectionService.SetDefaultViewport(GetDefaultViewportIdForLayout(_currentLayout));

            _activeViewportId = _viewportSelectionService.ActiveViewportId;

            _selectionService.SelectedSeriesChanged += OnSelectedSeriesChanged;
            _toolService.ActiveToolChanged += OnActiveToolChanged;
            _viewportService.StateChanged += OnViewportStateChanged;
            _layoutService.CurrentLayoutChanged += OnCurrentLayoutChanged;
            _viewportSelectionService.ActiveViewportChanged += OnActiveViewportChanged;

            _activeTool = _toolService.ActiveTool;

            SelectViewportCommand = new DelegateCommand<string?>(SelectViewport);

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

        public string ActiveViewportDisplayText
        {
            get
            {
                var viewports = _layoutService.GetViewportsForLayout(CurrentLayout);

                var activeViewport = viewports.FirstOrDefault(viewport => string.Equals(viewport.Id, ActiveViewportId, StringComparison.Ordinal));

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
                    _viewportService.SetSlice(value, SliceCount);
                }
            }
        }

        public bool IsAxialSagittalCoronalLayoutVisible => CurrentLayout == ImagingLayout.AxialSagittalCoronal;

        public bool IsEmptyViewerVisible => CurrentImage == null;

        public bool IsMprLayoutVisible => CurrentLayout == ImagingLayout.Mpr;

        public bool IsSingleLayoutVisible => CurrentLayout == ImagingLayout.Single;

        public bool IsTwoByTwoLayoutVisible => CurrentLayout == ImagingLayout.TwoByTwo;

        public string LayoutDisplayText => CurrentLayout switch
        {
            ImagingLayout.Single => "Layout: Einzelansicht",
            ImagingLayout.TwoByTwo => "Layout: 2 × 2",
            ImagingLayout.Mpr => "Layout: MPR",
            ImagingLayout.AxialSagittalCoronal => "Layout: Axial / Sagittal / Coronal",
            _ => "Layout: Unbekannt"
        };

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
            _selectionService.SelectedSeriesChanged -= OnSelectedSeriesChanged;
            _toolService.ActiveToolChanged -= OnActiveToolChanged;
            _viewportService.StateChanged -= OnViewportStateChanged;
            _layoutService.CurrentLayoutChanged -= OnCurrentLayoutChanged;
            _viewportSelectionService.ActiveViewportChanged -= OnActiveViewportChanged;

            base.Destroy();
        }

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

        private string GetDefaultViewportIdForLayout(ImagingLayout layout)
        {
            var viewports = _layoutService.GetViewportsForLayout(layout);

            var defaultViewport = viewports.FirstOrDefault(viewport => viewport.IsInteractive) ?? viewports.FirstOrDefault();

            return defaultViewport?.Id ?? "Single.Main";
        }

        private void OnActiveToolChanged(object? sender, ImagingToolChangedEventArgs e) => ActiveTool = e.NewTool;

        private void OnActiveViewportChanged(object? sender, ImagingViewportSelectionChangedEventArgs e) => ActiveViewportId = e.NewViewportId;

        private void OnCurrentLayoutChanged(object? sender, ImagingLayoutChangedEventArgs e)
        {
            CurrentLayout = e.NewLayout;

            _viewportSelectionService.SetDefaultViewport(GetDefaultViewportIdForLayout(e.NewLayout));
        }

        private void OnSelectedSeriesChanged(object? sender, SeriesSelectionChangedEventArgs e) => ApplySelectedSeries(e.SelectedSeries);

        private void OnViewportStateChanged(object? sender, ImagingViewportStateChangedEventArgs e) => ApplyViewportState(e.State);

        private void SelectViewport(string? viewportId)
        {
            if (string.IsNullOrWhiteSpace(viewportId))
            {
                return;
            }

            _viewportSelectionService.SelectViewport(viewportId);
        }
    }
}