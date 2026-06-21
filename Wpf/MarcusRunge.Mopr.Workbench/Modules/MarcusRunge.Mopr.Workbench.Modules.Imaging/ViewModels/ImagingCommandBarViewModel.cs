using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts.Dialog;
using Prism.Commands;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ImagingCommandBarViewModel : ViewModelBase
    {
        private readonly ICore _core;
        private readonly IWpf _wpf;
        private ImagingTool _activeTool;
        private ImagingLayout _currentLayout;
        private bool _isBusy;

        public ImagingCommandBarViewModel(ICore core, IWpf wpf)
        {
            _core = core;
            _wpf = wpf;

            _core.ImagingService!.ImagingToolService!.ActiveToolChanged += OnActiveToolChanged;
            _core.ImagingService!.ImagingLayoutService!.CurrentLayoutChanged += OnCurrentLayoutChanged;

            _activeTool = _core.ImagingService!.ImagingToolService!.ActiveTool;
            _currentLayout = _core.ImagingService!.ImagingLayoutService!.CurrentLayout;

            OpenCommand = new DelegateCommand(async () => await OpenAsync());
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

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OpenCommand.RaiseCanExecuteChanged();
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
            _core.ImagingService!.ImagingToolService!.ActiveToolChanged -= OnActiveToolChanged;
            _core.ImagingService!.ImagingLayoutService!.CurrentLayoutChanged -= OnCurrentLayoutChanged;

            base.Destroy();
        }

        private void ActivateCrosshair() => _core.ImagingService!.ImagingToolService!.SetActiveTool(ImagingTool.Crosshair);

        private void ActivatePan() => _core.ImagingService!.ImagingToolService!.SetActiveTool(ImagingTool.Pan);

        private void ActivateWindowLevel() => _core.ImagingService!.ImagingToolService!.SetActiveTool(ImagingTool.WindowLevel);

        private void ActivateZoom() => _core.ImagingService!.ImagingToolService!.SetActiveTool(ImagingTool.Zoom);

        private void ChangeLayout() => _core.ImagingService!.ImagingLayoutService!.CycleNextLayout();

        private void OnActiveToolChanged(object? sender, ImagingToolChangedEventArgs e) => ActiveTool = e.NewTool;

        private void OnCurrentLayoutChanged(object? sender, ImagingLayoutChangedEventArgs e) => CurrentLayout = e.NewLayout;

        private async Task OpenAsync()
        {
            if (IsBusy)
            {
                return;
            }

            try
            {
                IsBusy = true;

                var folderPath = _wpf.DialogService!.FileDialogService!.SelectFolder(
                    title: "DICOM-Ordner öffnen");

                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    return;
                }

                await _core.ImagingService!.ImagingStudyService!.LoadStudyFromFolderAsync(folderPath);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OpenMoreMenu()
        {
        }

        private void ResetView()
        {
            _core.ImagingService!.ImagingViewportService!.Reset();
            _core.ImagingService!.ImagingToolService!.ClearActiveTool();
        }
    }
}