using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
using Prism.Commands;
using System;
using System.Threading;
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
        private CancellationTokenSource? _openCancellationTokenSource;
        private string _statusText = Resources.CommandBar_Status;

        public ImagingCommandBarViewModel(ICore core, IWpf wpf)
        {
            _core = core;
            _wpf = wpf;

            _core.ImagingService!.ImagingToolService!.ActiveToolChanged += OnActiveToolChanged;
            _core.ImagingService!.ImagingLayoutService!.CurrentLayoutChanged += OnCurrentLayoutChanged;

            _activeTool = _core.ImagingService!.ImagingToolService!.ActiveTool;
            _currentLayout = _core.ImagingService!.ImagingLayoutService!.CurrentLayout;

            BoneWindowCommand = new DelegateCommand(ApplyBoneWindow);
            BrainWindowCommand = new DelegateCommand(ApplyBrainWindow);
            CancelOpenCommand = new DelegateCommand(CancelOpen, () => IsBusy);
            CrosshairCommand = new DelegateCommand(ActivateCrosshair);
            LayoutCommand = new DelegateCommand(ChangeLayout);
            LungWindowCommand = new DelegateCommand(ApplyLungWindow);
            MediastinumWindowCommand = new DelegateCommand(ApplyMediastinumWindow);
            MoreCommand = new DelegateCommand(OpenMoreMenu);
            OpenCommand = new DelegateCommand(async () => await OpenAsync(), () => !IsBusy);
            PanCommand = new DelegateCommand(ActivatePan);
            ResetViewCommand = new DelegateCommand(ResetView);
            ResetWindowLevelToDefaultCommand = new DelegateCommand(ResetWindowLevelToDefault);
            WindowLevelCommand = new DelegateCommand(ActivateWindowLevel);
            ZoomCommand = new DelegateCommand(ActivateZoom);
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

        public DelegateCommand CancelOpenCommand { get; }

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

        public bool HasStatusText => !string.IsNullOrWhiteSpace(StatusText);

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OpenCommand.RaiseCanExecuteChanged();
                    CancelOpenCommand.RaiseCanExecuteChanged();
                    RaisePropertyChanged(nameof(OpenButtonText));
                    RaisePropertyChanged(nameof(IsCancelVisible));
                }
            }
        }

        public bool IsCancelVisible => IsBusy;

        public bool IsCrosshairActive => ActiveTool == ImagingTool.Crosshair;

        public bool IsPanActive => ActiveTool == ImagingTool.Pan;

        public bool IsWindowLevelActive => ActiveTool == ImagingTool.WindowLevel;

        public bool IsZoomActive => ActiveTool == ImagingTool.Zoom;

        public DelegateCommand LayoutCommand { get; }

        public string LayoutDisplayText => CurrentLayout switch
        {
            ImagingLayout.Single => Resources.CommandBar_Layout_Single,
            ImagingLayout.TwoByTwo => Resources.CommandBar_Layout_TwoByTwo,
            ImagingLayout.Mpr => Resources.CommandBar_Layout_Mpr,
            ImagingLayout.AxialSagittalCoronal => Resources.CommandBar_Layout_AxialSagittalCoronal,
            _ => Resources.CommandBar_Layout
        };

        public DelegateCommand MoreCommand { get; }

        public string OpenButtonText => IsBusy ? Resources.CommandBar_Load : Resources.CommandBar_Open;

        public DelegateCommand OpenCommand { get; }

        public DelegateCommand PanCommand { get; }

        public DelegateCommand ResetViewCommand { get; }

        public string StatusText
        {
            get => _statusText;
            private set
            {
                if (SetProperty(ref _statusText, value))
                {
                    RaisePropertyChanged(nameof(HasStatusText));
                }
            }
        }

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

        private void CancelOpen()
        {
            if (_openCancellationTokenSource == null)
            {
                return;
            }

            StatusText = Resources.CommandBar_Canceling;
            _openCancellationTokenSource.Cancel();
        }

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
                StatusText = Resources.CommandBar_SelectFolder;

                var folderPath = _wpf.DialogService!.FileDialogService!.SelectFolder(title: Resources.CommandBar_Open_DicomFolder);

                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    StatusText = string.Empty;
                    return;
                }

                _openCancellationTokenSource = new CancellationTokenSource();

                var progress = new Progress<ImagingStudyLoadProgress>(value =>
                {
                    StatusText = value.DisplayText;
                });

                await _core.ImagingService!.ImagingStudyService!.LoadStudyFromFolderAsync(folderPath, progress, _openCancellationTokenSource.Token);

                if (_openCancellationTokenSource.IsCancellationRequested)
                {
                    StatusText = Resources.CommandBar_LoadCanceled;
                    return;
                }

                var summary = _core.ImagingService!.ImagingStudyService!.LastScanSummary;

                StatusText = summary?.DisplayText ?? string.Empty;
            }
            catch (Exception exception)
            {
                StatusText = Resources.CommandBar_LoadError;
                System.Diagnostics.Debug.WriteLine(exception);
            }
            finally
            {
                _openCancellationTokenSource?.Dispose();
                _openCancellationTokenSource = null;

                IsBusy = false;
            }
        }

        private void OpenMoreMenu()
        {
        }

        private void ResetView()
        {
            _core.ImagingService!.ImagingViewportService!.Reset();
            _core.ImagingService!.ImagingWindowLevelService!.ResetWindowLevelToDefault();
            _core.ImagingService!.ImagingToolService!.ClearActiveTool();
        }

        public DelegateCommand LungWindowCommand { get; }

        public DelegateCommand MediastinumWindowCommand { get; }

        public DelegateCommand BoneWindowCommand { get; }

        public DelegateCommand BrainWindowCommand { get; }

        public DelegateCommand ResetWindowLevelToDefaultCommand { get; }

        private void ApplyLungWindow() => _core.ImagingService!.ImagingWindowLevelService!.SetWindowLevel(windowCenter: -600, windowWidth: 1500);

        private void ApplyMediastinumWindow() => _core.ImagingService!.ImagingWindowLevelService!.SetWindowLevel(windowCenter: 40, windowWidth: 400);

        private void ApplyBoneWindow() => _core.ImagingService!.ImagingWindowLevelService!.SetWindowLevel(windowCenter: 300, windowWidth: 1500);

        private void ApplyBrainWindow() => _core.ImagingService!.ImagingWindowLevelService!.SetWindowLevel(windowCenter: 40, windowWidth: 80);

        private void ResetWindowLevelToDefault() => _core.ImagingService!.ImagingWindowLevelService!.ResetWindowLevelToDefault();
    }
}