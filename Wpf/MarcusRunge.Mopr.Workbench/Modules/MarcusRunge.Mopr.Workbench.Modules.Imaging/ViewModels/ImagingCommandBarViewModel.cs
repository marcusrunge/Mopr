using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging;
using Prism.Commands;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ImagingCommandBarViewModel : ViewModelBase
    {
        private readonly IImagingToolService _toolService;

        private ImagingTool _activeTool;

        public ImagingCommandBarViewModel(IImagingToolService toolService)
        {
            _toolService = toolService;
            _toolService.ActiveToolChanged += OnActiveToolChanged;

            _activeTool = _toolService.ActiveTool;

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
        public bool IsCrosshairActive => ActiveTool == ImagingTool.Crosshair;
        public bool IsPanActive => ActiveTool == ImagingTool.Pan;
        public bool IsWindowLevelActive => ActiveTool == ImagingTool.WindowLevel;
        public bool IsZoomActive => ActiveTool == ImagingTool.Zoom;
        public DelegateCommand LayoutCommand { get; }
        public DelegateCommand MoreCommand { get; }
        public DelegateCommand OpenCommand { get; }
        public DelegateCommand PanCommand { get; }
        public DelegateCommand ResetViewCommand { get; }
        public DelegateCommand WindowLevelCommand { get; }
        public DelegateCommand ZoomCommand { get; }

        public override void Destroy()
        {
            _toolService.ActiveToolChanged -= OnActiveToolChanged;
            base.Destroy();
        }

        private void ActivateCrosshair() => _toolService.SetActiveTool(ImagingTool.Crosshair);

        private void ActivatePan() => _toolService.SetActiveTool(ImagingTool.Pan);

        private void ActivateWindowLevel() => _toolService.SetActiveTool(ImagingTool.WindowLevel);

        private void ActivateZoom() => _toolService.SetActiveTool(ImagingTool.Zoom);

        private void ChangeLayout()
        {
        }

        private void OnActiveToolChanged(object? sender, ImagingToolChangedEventArgs e) => ActiveTool = e.NewTool;

        private void Open()
        {
        }

        private void OpenMoreMenu()
        {
        }

        private void ResetView() => _toolService.ClearActiveTool();
    }
}