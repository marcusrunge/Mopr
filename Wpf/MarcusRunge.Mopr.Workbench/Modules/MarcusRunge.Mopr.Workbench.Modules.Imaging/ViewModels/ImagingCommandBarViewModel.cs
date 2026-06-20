using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging;
using Prism.Commands;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ImagingCommandBarViewModel : ViewModelBase
    {
        private readonly IImagingLayoutService _layoutService;
        private readonly IImagingStudyService _studyService;
        private readonly IImagingToolService _toolService;
        private readonly IImagingViewportService _viewportService;
        private ImagingTool _activeTool;

        private ImagingLayout _currentLayout;

        public ImagingCommandBarViewModel(IImagingToolService toolService, IImagingLayoutService layoutService, IImagingViewportService viewportService, IImagingStudyService studyService)

        {
            _toolService = toolService;
            _layoutService = layoutService;
            _viewportService = viewportService;
            _studyService = studyService;

            _toolService.ActiveToolChanged += OnActiveToolChanged;
            _layoutService.CurrentLayoutChanged += OnCurrentLayoutChanged;

            _activeTool = _toolService.ActiveTool;
            _currentLayout = _layoutService.CurrentLayout;

            OpenCommand = new DelegateCommand(Open);
            LayoutCommand = new DelegateCommand(ChangeLayout);

            ZoomCommand = new DelegateCommand(ActivateZoom);
            PanCommand = new DelegateCommand(ActivatePan);
            WindowLevelCommand = new DelegateCommand(ActivateWindowLevel);
            CrosshairCommand = new DelegateCommand(ActivateCrosshair);

            ResetViewCommand = new DelegateCommand(ResetView);
            MoreCommand = new DelegateCommand(OpenMoreMenu);
        }

        public ImagingTool ActiveTool
        {
            get => _activeTool;
            private set
            {
                if (SetProperty(ref _activeTool, value))
                {
                    RaisePropertyChanged(nameof(IsZoomActive));
                    RaisePropertyChanged(nameof(IsPanActive));
                    RaisePropertyChanged(nameof(IsWindowLevelActive));
                    RaisePropertyChanged(nameof(IsCrosshairActive));
                }
            }
        }

        public DelegateCommand CrosshairCommand { get; }

        public ImagingLayout CurrentLayout
        {
            get => _currentLayout;
            private set
            {
                if (SetProperty(ref _currentLayout, value))
                {
                    RaisePropertyChanged(nameof(LayoutDisplayText));
                }
            }
        }

        public bool IsCrosshairActive => ActiveTool == ImagingTool.Crosshair;

        public bool IsPanActive => ActiveTool == ImagingTool.Pan;

        public bool IsWindowLevelActive => ActiveTool == ImagingTool.WindowLevel;

        public bool IsZoomActive => ActiveTool == ImagingTool.Zoom;

        public DelegateCommand LayoutCommand { get; }

        public string LayoutDisplayText => CurrentLayout switch
        {
            ImagingLayout.Single => "Layout: Einzel",
            ImagingLayout.TwoByTwo => "Layout: 2 × 2",
            ImagingLayout.Mpr => "Layout: MPR",
            ImagingLayout.AxialSagittalCoronal => "Layout: A/S/C",
            _ => "Layout"
        };

        public DelegateCommand MoreCommand { get; }
        public DelegateCommand OpenCommand { get; }
        public DelegateCommand PanCommand { get; }
        public DelegateCommand ResetViewCommand { get; }
        public DelegateCommand WindowLevelCommand { get; }
        public DelegateCommand ZoomCommand { get; }

        public override void Destroy()
        {
            _toolService.ActiveToolChanged -= OnActiveToolChanged;
            _layoutService.CurrentLayoutChanged -= OnCurrentLayoutChanged;

            base.Destroy();
        }

        private void ActivateCrosshair() => _toolService.SetActiveTool(ImagingTool.Crosshair);

        private void ActivatePan() => _toolService.SetActiveTool(ImagingTool.Pan);

        private void ActivateWindowLevel() => _toolService.SetActiveTool(ImagingTool.WindowLevel);

        private void ActivateZoom() => _toolService.SetActiveTool(ImagingTool.Zoom);

        private void ChangeLayout() => _layoutService.CycleNextLayout();

        private void OnActiveToolChanged(object? sender, ImagingToolChangedEventArgs e) => ActiveTool = e.NewTool;

        private void OnCurrentLayoutChanged(object? sender, ImagingLayoutChangedEventArgs e) => CurrentLayout = e.NewLayout;

        private void Open() => _studyService.LoadDemoStudy();

        private void OpenMoreMenu()
        {
        }

        private void ResetView()
        {
            _viewportService.Reset();
            _toolService.ClearActiveTool();
        }
    }
}