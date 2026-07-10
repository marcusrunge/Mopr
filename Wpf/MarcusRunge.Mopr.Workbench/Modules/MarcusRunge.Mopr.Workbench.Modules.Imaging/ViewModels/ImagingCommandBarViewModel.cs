using MarcusRunge.Mopr.Workbench.Contracts.Enums;
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
        private DelegateCommand? _applyCtBoneWindowCommand, _applyCtBrainWindowCommand, _cancelOpenCommand, _crosshairCommand, _layoutCommand, _applyCtLungWindowCommand, _measureCommand, _applyCtMediastinumWindowCommand, _moreCommand, _openCommand, _panCommand, _resetViewCommand, _resetWindowLevelToDefaultCommand, _windowLevelCommand, _zoomCommand;
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
                    RaisePropertyChanged(nameof(IsMeasureActive));
                }
            }
        }

        public DelegateCommand ApplyCtBoneWindowCommand => _applyCtBoneWindowCommand ??= new DelegateCommand(ApplyBoneWindow);
        public DelegateCommand ApplyCtBrainWindowCommand => _applyCtBrainWindowCommand ??= new DelegateCommand(ApplyBrainWindow);
        public DelegateCommand ApplyCtLungWindowCommand => _applyCtLungWindowCommand ??= new DelegateCommand(ApplyLungWindow);
        public DelegateCommand ApplyCtMediastinumWindowCommand => _applyCtMediastinumWindowCommand ??= new DelegateCommand(ApplyMediastinumWindow);        
        public DelegateCommand CancelOpenCommand => _cancelOpenCommand ??= new DelegateCommand(CancelOpen, CanCancelOpen);

        public DelegateCommand CrosshairCommand => _crosshairCommand ??= new DelegateCommand(ActivateCrosshair);

        public ImagingLayout CurrentLayout
        {
            get => _currentLayout;
            private set
            {
                if (SetProperty(ref _currentLayout, value))
                {
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
                    _openCommand?.RaiseCanExecuteChanged();
                    _cancelOpenCommand?.RaiseCanExecuteChanged();
                    RaisePropertyChanged(nameof(OpenButtonText));
                    RaisePropertyChanged(nameof(IsCancelVisible));
                }
            }
        }

        public bool IsCancelVisible => IsBusy;

        public bool IsCrosshairActive => ActiveTool == ImagingTool.Crosshair;

        public bool IsMeasureActive => ActiveTool == ImagingTool.Measure;

        public bool IsPanActive => ActiveTool == ImagingTool.Pan;

        public bool IsWindowLevelActive => ActiveTool == ImagingTool.WindowLevel;

        public bool IsZoomActive => ActiveTool == ImagingTool.Zoom;

        public DelegateCommand LayoutCommand => _layoutCommand ??= new DelegateCommand(ChangeLayout);

        public DelegateCommand MeasureCommand => _measureCommand ??= new DelegateCommand(ActivateMeasure);

        public DelegateCommand MoreCommand => _moreCommand ??= new DelegateCommand(OpenMoreMenu);

        public string OpenButtonText => IsBusy ? Resources.CommandBar_Load : Resources.CommandBar_Open;

        public DelegateCommand OpenCommand => _openCommand ??= new DelegateCommand(async () => await OpenAsync(), CanOpen);

        public DelegateCommand PanCommand => _panCommand ??= new DelegateCommand(ActivatePan);

        public DelegateCommand ResetViewCommand => _resetViewCommand ??= new DelegateCommand(ResetView);

        public DelegateCommand ResetWindowLevelToDefaultCommand => _resetWindowLevelToDefaultCommand ??= new DelegateCommand(ResetWindowLevelToDefault);

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

        public DelegateCommand WindowLevelCommand => _windowLevelCommand ??= new DelegateCommand(ActivateWindowLevel);

        public DelegateCommand ZoomCommand => _zoomCommand ??= new DelegateCommand(ActivateZoom);

        public override void Destroy()
        {
            _core.ImagingService!.ImagingToolService!.ActiveToolChanged -= OnActiveToolChanged;
            _core.ImagingService!.ImagingLayoutService!.CurrentLayoutChanged -= OnCurrentLayoutChanged;

            base.Destroy();
        }

        private void ActivateCrosshair() => _core.ImagingService!.ImagingToolService!.SetActiveTool(ImagingTool.Crosshair);

        private void ActivateMeasure()
        {
            if (ActiveTool == ImagingTool.Measure)
            {
                _core.ImagingService!.ImagingToolService!.ClearActiveTool();
                return;
            }
            _core.ImagingService!.ImagingToolService!.SetActiveTool(ImagingTool.Measure);
        }

        private void ActivatePan() => _core.ImagingService!.ImagingToolService!.SetActiveTool(ImagingTool.Pan);

        private void ActivateWindowLevel() => _core.ImagingService!.ImagingToolService!.SetActiveTool(ImagingTool.WindowLevel);

        private void ActivateZoom() => _core.ImagingService!.ImagingToolService!.SetActiveTool(ImagingTool.Zoom);

        private void ApplyBoneWindow() => _core.ImagingService!.ImagingWindowLevelService!.SetWindowLevel(windowCenter: 300, windowWidth: 1500);

        private void ApplyBrainWindow() => _core.ImagingService!.ImagingWindowLevelService!.SetWindowLevel(windowCenter: 40, windowWidth: 80);

        private void ApplyLungWindow() => _core.ImagingService!.ImagingWindowLevelService!.SetWindowLevel(windowCenter: -600, windowWidth: 1500);

        private void ApplyMediastinumWindow() => _core.ImagingService!.ImagingWindowLevelService!.SetWindowLevel(windowCenter: 40, windowWidth: 400);

        private bool CanCancelOpen() => IsBusy;

        private void CancelOpen()
        {
            if (_openCancellationTokenSource == null)
            {
                return;
            }

            StatusText = Resources.CommandBar_Canceling;
            _openCancellationTokenSource.Cancel();
        }

        private bool CanOpen() => !IsBusy;

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

        private void ResetWindowLevelToDefault() => _core.ImagingService!.ImagingWindowLevelService!.ResetWindowLevelToDefault();
    }
}