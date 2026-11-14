using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using Prism.Commands;
using Prism.Dialogs;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrismDialogService = Prism.Dialogs.IDialogService;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ImagingCommandBarViewModel : ViewModelBase
    {
        private const string ImportDialogName = "ImportView";
        private const string SelectedSeriesRequestsParameterName = "SelectedSeriesRequests";
        private const string StudySelectionDialogName = "StudySelectionView";
        private const string UnsavedChangesDialogName = "UnsavedChangesDialogView";
        private readonly ICore _core;
        private readonly PrismDialogService _dialogService;
        private readonly IImagingSessionService _imagingSessionService;
        private readonly IImportedSeriesLoadService _importedSeriesLoader;
        private readonly IApplication _wpf;
        private ImagingTool _activeTool;
        private DelegateCommand? _applyCtBoneWindowCommand, _applyCtBrainWindowCommand, _applyCtLungWindowCommand, _applyCtMediastinumWindowCommand, _cancelOpenCommand, _closeStudyCommand, _crosshairCommand, _importCommand, _layoutCommand, _measureCommand, _moreCommand, _openCommand, _openImportedSeriesCommand, _panCommand, _resetViewCommand, _resetWindowLevelToDefaultCommand, _windowLevelCommand, _zoomCommand;
        private ImagingLayout _currentLayout;

        private bool _isBusy;

        private CancellationTokenSource? _openCancellationTokenSource;

        private string _statusText = Resources.CommandBar_Status;

        public ImagingCommandBarViewModel(ICore core, IApplication wpf, PrismDialogService dialogService)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _wpf = wpf ?? throw new ArgumentNullException(nameof(wpf));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _importedSeriesLoader = _wpf.StudiesService?.ImportedSeriesLoader ?? throw new InvalidOperationException("The imported-series loader has not been initialized.");
            _imagingSessionService = _core.ImagingService?.ImagingSessionService ?? throw new InvalidOperationException("The imaging session service has not been initialized.");

            _core.ImagingService!.ImagingToolService!.ActiveToolChanged += OnActiveToolChanged;
            _imagingSessionService.StateChanged += OnImagingSessionStateChanged;
            _core.ImagingService.ImagingLayoutService!.CurrentLayoutChanged += OnCurrentLayoutChanged;

            _activeTool = _core.ImagingService.ImagingToolService.ActiveTool;
            _currentLayout = _core.ImagingService.ImagingLayoutService.CurrentLayout;
        }

        public ImagingTool ActiveTool
        {
            get => _activeTool;
            private set
            {
                if (!SetProperty(ref _activeTool, value))
                {
                    return;
                }

                RaisePropertyChanged(nameof(IsZoomActive));
                RaisePropertyChanged(nameof(IsPanActive));
                RaisePropertyChanged(nameof(IsWindowLevelActive));
                RaisePropertyChanged(nameof(IsCrosshairActive));
                RaisePropertyChanged(nameof(IsMeasureActive));
            }
        }

        public DelegateCommand ApplyCtBoneWindowCommand => _applyCtBoneWindowCommand ??= new DelegateCommand(ApplyBoneWindow);

        public DelegateCommand ApplyCtBrainWindowCommand => _applyCtBrainWindowCommand ??= new DelegateCommand(ApplyBrainWindow);

        public DelegateCommand ApplyCtLungWindowCommand => _applyCtLungWindowCommand ??= new DelegateCommand(ApplyLungWindow);

        public DelegateCommand ApplyCtMediastinumWindowCommand => _applyCtMediastinumWindowCommand ??= new DelegateCommand(ApplyMediastinumWindow);

        public DelegateCommand CancelOpenCommand => _cancelOpenCommand ??= new DelegateCommand(CancelOpen, CanCancelOpen);

        public DelegateCommand CloseStudyCommand => _closeStudyCommand ??= new DelegateCommand(CloseStudy, CanCloseStudy);

        public DelegateCommand CrosshairCommand => _crosshairCommand ??= new DelegateCommand(ActivateCrosshair);

        public ImagingLayout CurrentLayout
        {
            get => _currentLayout;
            private set => SetProperty(ref _currentLayout, value);
        }

        public bool HasStatusText => !string.IsNullOrWhiteSpace(StatusText);

        public DelegateCommand ImportCommand => _importCommand ??= new DelegateCommand(OpenImport, CanOpenImport);

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (!SetProperty(ref _isBusy, value))
                {
                    return;
                }

                _openCommand?.RaiseCanExecuteChanged();
                _openImportedSeriesCommand?.RaiseCanExecuteChanged();
                _importCommand?.RaiseCanExecuteChanged();
                _cancelOpenCommand?.RaiseCanExecuteChanged();
                _closeStudyCommand?.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(OpenButtonText));
                RaisePropertyChanged(nameof(IsCancelVisible));
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
        public DelegateCommand OpenImportedSeriesCommand => _openImportedSeriesCommand ??= new DelegateCommand(OpenImportedSeries, CanOpenImportedSeries);
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
            _core.ImagingService.ImagingLayoutService!.CurrentLayoutChanged -= OnCurrentLayoutChanged;
            _imagingSessionService.StateChanged -= OnImagingSessionStateChanged;

            _openCancellationTokenSource?.Cancel();
            _openCancellationTokenSource?.Dispose();
            _openCancellationTokenSource = null;

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
            if (_openCancellationTokenSource is null)
            {
                return;
            }

            StatusText = Resources.CommandBar_Canceling;
            _openCancellationTokenSource.Cancel();
        }

        private bool CanCloseStudy() => !IsBusy && _imagingSessionService.HasOpenStudy;

        private bool CanOpen() => !IsBusy;

        private bool CanOpenImport() => !IsBusy;

        private bool CanOpenImportedSeries() => !IsBusy;

        private void ChangeLayout() => _core.ImagingService!.ImagingLayoutService!.CycleNextLayout();

        private void CloseStudy()
        {
            if (!CanCloseStudy()) return;

            if (_imagingSessionService.HasUnsavedChanges)
            {
                _dialogService.ShowDialog(UnsavedChangesDialogName, result =>
                {
                    if (result.Result != ButtonResult.OK) return;
                    _imagingSessionService.DiscardChangesAndClear();
                    StatusText = Resources.CommandBar_StudyClosed;
                });
                return;
            }

            _imagingSessionService.Clear();
            StatusText = Resources.CommandBar_StudyClosed;
        }

        private async Task<bool> ConfirmDiscardUnsavedChangesAsync()
        {
            if (!_imagingSessionService.HasUnsavedChanges)
            {
                return true;
            }

            var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _dialogService.ShowDialog(UnsavedChangesDialogName, result => completionSource.TrySetResult(result.Result == ButtonResult.OK));

            bool discardConfirmed = await completionSource.Task;

            if (!discardConfirmed)
            {
                return false;
            }

            // Discarding is deliberately separated from the normal Clear path.
            // Only an explicit user confirmation may remove registered changes.
            _imagingSessionService.DiscardChangesAndClear();

            return true;
        }

        private async void ExecuteOpenImportedSeries(IDialogResult dialogResult)
        {
            try
            {
                await OpenImportedSeriesAsync(dialogResult);
            }
            catch (Exception exception)
            {
                StatusText = Resources.CommandBar_LoadError;
                System.Diagnostics.Debug.WriteLine(exception);
            }
        }

        private void OnActiveToolChanged(object? sender, ImagingToolChangedEventArgs e) => ActiveTool = e.NewTool;

        private void OnCurrentLayoutChanged(object? sender, ImagingLayoutChangedEventArgs e) => CurrentLayout = e.NewLayout;

        private void OnImagingSessionStateChanged(object? sender, EventArgs e) => _closeStudyCommand?.RaiseCanExecuteChanged();

        private async Task OpenAsync()
        {
            if (IsBusy)
            {
                return;
            }

            string previousStatusText = StatusText;

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

                if (!await ConfirmDiscardUnsavedChangesAsync())
                {
                    StatusText = previousStatusText;
                    return;
                }

                _openCancellationTokenSource = new CancellationTokenSource();

                var progress = new Progress<ImagingStudyLoadProgress>(value => StatusText = value.DisplayText);

                await _core.ImagingService!.ImagingStudyService!.LoadStudyFromFolderAsync(
                    folderPath,
                    progress,
                    _openCancellationTokenSource.Token);

                if (_openCancellationTokenSource.IsCancellationRequested)
                {
                    StatusText = Resources.CommandBar_LoadCanceled;
                    return;
                }

                var summary = _core.ImagingService.ImagingStudyService.LastScanSummary;

                StatusText = summary?.DisplayText ?? string.Empty;
            }
            catch (OperationCanceledException) when (_openCancellationTokenSource?.IsCancellationRequested == true)
            {
                StatusText = Resources.CommandBar_LoadCanceled;
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

        private void OpenImport()
        {
            if (!CanOpenImport())
            {
                return;
            }

            _dialogService.ShowDialog(ImportDialogName);
        }

        private void OpenImportedSeries()
        {
            if (!CanOpenImportedSeries()) return;

            _dialogService.ShowDialog(StudySelectionDialogName, result =>
            {
                if (result.Result != ButtonResult.OK) return;
                ExecuteOpenImportedSeries(result);
            });
        }

        private async Task OpenImportedSeriesAsync(IDialogResult dialogResult)
        {
            if (IsBusy)
            {
                return;
            }

            ImportedSeriesLoadRequest[] requests =
                dialogResult.Parameters.GetValue<ImportedSeriesLoadRequest[]>(SelectedSeriesRequestsParameterName) ?? [];

            if (requests.Length == 0)
            {
                StatusText = Resources.CommandBar_ImportedSeriesSelectionInvalid;
                return;
            }

            string previousStatusText = StatusText;

            try
            {
                IsBusy = true;
                StatusText = Resources.CommandBar_ImportedSeriesResolving;
                _openCancellationTokenSource = new CancellationTokenSource();

                var progress = new Progress<ImagingStudyLoadProgress>(value => StatusText = value.Message);

                ImportedStudyLoadResult loadResult = await _importedSeriesLoader.ResolveAsync(
                    requests,
                    _openCancellationTokenSource.Token);

                if (_openCancellationTokenSource.IsCancellationRequested)
                {
                    StatusText = Resources.CommandBar_LoadCanceled;
                    return;
                }

                if (!loadResult.IsSuccessful ||
                    string.IsNullOrWhiteSpace(loadResult.StudyInstanceUid) ||
                    loadResult.Series.Count == 0)
                {
                    foreach (var technicalError in loadResult.TechnicalErrors)
                    {
                        System.Diagnostics.Debug.WriteLine(technicalError);
                    }

                    StatusText = Resources.CommandBar_ImportedSeriesUnavailable;
                    return;
                }

                ImagingSeriesLoadRequest[] imagingRequests =
                [
                    .. loadResult.Series.Select(series =>
                new ImagingSeriesLoadRequest(series.SeriesInstanceUid!, series.Files))
                ];

                // Resolve and validate the complete target study before asking the user
                // to discard the current session. Invalid repository data must never
                // cause existing unsaved changes to be lost.
                if (!await ConfirmDiscardUnsavedChangesAsync())
                {
                    StatusText = previousStatusText;
                    return;
                }

                await _core.ImagingService!.ImagingStudyService!.LoadStudyFromSeriesAsync(
                    loadResult.StudyInstanceUid,
                    imagingRequests,
                    progress,
                    _openCancellationTokenSource.Token);

                StatusText = Resources.CommandBar_ImportedSeriesLoaded;
            }
            catch (OperationCanceledException) when (_openCancellationTokenSource?.IsCancellationRequested == true)
            {
                StatusText = Resources.CommandBar_LoadCanceled;
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
            _core.ImagingService.ImagingWindowLevelService!.ResetWindowLevelToDefault();
            _core.ImagingService.ImagingToolService!.ClearActiveTool();
        }

        private void ResetWindowLevelToDefault() => _core.ImagingService!.ImagingWindowLevelService!.ResetWindowLevelToDefault();
    }
}