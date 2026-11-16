using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Import;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace MarcusRunge.Mopr.Workbench.Modules.Import.ViewModels
{
    /// <summary>
    /// Coordinates source selection and user-initiated DICOM import operations.
    /// </summary>
    public sealed class ImportViewModel : ViewModelBase, IDialogAware
    {
        private readonly IDicomImportDriveProvider _dicomImportDriveProvider;
        private readonly IImportService _importService;
        private readonly IDicomImportService _dicomImportService;
        private readonly DispatcherTimer _discoveryTimer;
        private readonly IFileDialogService _fileDialogService;
        private int _discoveryElapsedSeconds;
        private CancellationTokenSource? _driveRefreshCancellation;
        private CancellationTokenSource? _importCancellation;
        private CancellationTokenSource? _networkPathCancellation;
        private double _importProgressPercentage;
        private bool _isCancellationRequested;
        private bool _isImporting;
        private bool _isInspectingSource;
        private bool _isCheckingNetworkPath;
        private bool _isRefreshingDrives;
        private string _networkPath = string.Empty;
        private string _networkPathStatusText = string.Empty;
        private int _processedFiles;
        private ImportResultViewModel? _result;
        private int _totalFiles;

        public ImportViewModel(IApplication application)
        {
            ArgumentNullException.ThrowIfNull(application);

            _fileDialogService = application.DialogService?.FileDialogService ?? throw new InvalidOperationException("The file dialog service has not been initialized.");

            _importService = application.ImportService ?? throw new InvalidOperationException("The import service has not been initialized.");
            _dicomImportDriveProvider = _importService.DicomImportDriveProvider ?? throw new InvalidOperationException("The DICOM import drive provider has not been initialized.");
            _dicomImportService = _importService.DicomImportService ?? throw new InvalidOperationException("The DICOM import service has not been initialized.");

            AvailableDrives = [];
            AvailableDrives.CollectionChanged += OnAvailableDrivesCollectionChanged;

            Source = new ImportSourceViewModel();

            RefreshDrivesCommand = new DelegateCommand(ExecuteRefreshDrives, CanRefreshDrives);
            SelectDriveCommand = new DelegateCommand<ImportDriveItemViewModel>(SelectDrive, CanSelectDrive);
            SelectSourceCommand = new DelegateCommand(SelectSource, CanSelectSource);
            StartImportCommand = new DelegateCommand(ExecuteStartImport, CanStartImport);
            CancelImportCommand = new DelegateCommand(CancelImport, CanCancelImport);
            StartNewImportCommand = new DelegateCommand(StartNewImport, CanStartNewImport);
            CloseDialogCommand = new DelegateCommand(CloseDialog, CanCloseDialog);
            CheckNetworkPathCommand = new DelegateCommand(ExecuteCheckNetworkPath, CanCheckNetworkPath);

            _discoveryTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _discoveryTimer.Tick += OnDiscoveryTimerTick;
        }

        public static string Title => Properties.Resources.ImportTitle;

        public ObservableCollection<ImportDriveItemViewModel> AvailableDrives { get; }

        public string CancelImportButtonText => IsCancellationRequested ? Properties.Resources.ImportCancellationRequestedButton : Properties.Resources.CancelImportButton;

        public DelegateCommand CancelImportCommand { get; }

        public DelegateCommand CheckNetworkPathCommand { get; }

        public DelegateCommand CloseDialogCommand { get; }

        public int DiscoveredFiles => IsProgressIndeterminate ? ProcessedFiles : TotalFiles;

        public int DiscoveryElapsedSeconds
        {
            get => _discoveryElapsedSeconds;
            private set
            {
                if (!SetProperty(ref _discoveryElapsedSeconds, value))
                {
                    return;
                }

                RaisePropertyChanged(nameof(ImportProgressText));
            }
        }

        public string DriveSearchFoundCountText => string.Format(Properties.Resources.DriveSearchFoundCountFormat, AvailableDrives.Count);

        public bool HasResult => Result is not null;

        public double ImportProgressPercentage
        {
            get => _importProgressPercentage;
            private set => SetProperty(ref _importProgressPercentage, value);
        }

        public string ImportProgressText
        {
            get
            {
                if (IsCancellationRequested)
                {
                    return Properties.Resources.ImportCancellationRequestedMessage;
                }

                if (TotalFiles <= 0)
                {
                    return ProcessedFiles <= 0 ? string.Format(Properties.Resources.ImportProgressDiscoveringFormat, DiscoveryElapsedSeconds) : string.Format(Properties.Resources.ImportProgressDiscoveredFormat, ProcessedFiles);
                }

                return IsInspectingSource ? string.Format(Properties.Resources.ImportProgressInspectingFormat, ProcessedFiles, TotalFiles) : string.Format(Properties.Resources.ImportProgressProcessingFormat, ProcessedFiles, TotalFiles);
            }
        }

        public bool IsCancellationRequested
        {
            get => _isCancellationRequested;
            private set
            {
                if (!SetProperty(ref _isCancellationRequested, value))
                {
                    return;
                }

                RaisePropertyChanged(nameof(CancelImportButtonText));
                RaisePropertyChanged(nameof(ImportProgressText));
                RaiseCommandState();
            }
        }

        public bool IsImporting
        {
            get => _isImporting;
            private set
            {
                if (!SetProperty(ref _isImporting, value))
                {
                    return;
                }

                RaiseCommandState();
            }
        }

        public bool IsInspectingSource
        {
            get => _isInspectingSource;
            private set
            {
                if (!SetProperty(ref _isInspectingSource, value))
                {
                    return;
                }

                RaisePropertyChanged(nameof(ImportProgressText));
            }
        }

        public bool IsProgressIndeterminate => TotalFiles <= 0;

        public bool IsRefreshingDrives
        {
            get => _isRefreshingDrives;
            private set
            {
                if (!SetProperty(ref _isRefreshingDrives, value))
                {
                    return;
                }

                RaiseCommandState();
            }
        }

        public bool IsCheckingNetworkPath
        {
            get => _isCheckingNetworkPath;
            private set
            {
                if (!SetProperty(ref _isCheckingNetworkPath, value))
                {
                    return;
                }

                RaiseCommandState();
            }
        }

        public string NetworkPath
        {
            get => _networkPath;
            set
            {
                if (!SetProperty(ref _networkPath, value))
                {
                    return;
                }

                NetworkPathStatusText = string.Empty;
                CheckNetworkPathCommand.RaiseCanExecuteChanged();
            }
        }

        public string NetworkPathStatusText
        {
            get => _networkPathStatusText;
            private set => SetProperty(ref _networkPathStatusText, value);
        }

        public int ProcessedFiles
        {
            get => _processedFiles;
            private set
            {
                if (!SetProperty(ref _processedFiles, value))
                {
                    return;
                }

                RaisePropertyChanged(nameof(DiscoveredFiles));
                RaisePropertyChanged(nameof(ImportProgressText));
            }
        }

        public DelegateCommand RefreshDrivesCommand { get; }

        /// <inheritdoc/>
        public DialogCloseListener RequestClose { get; }

        public ImportResultViewModel? Result
        {
            get => _result;
            private set
            {
                if (!SetProperty(ref _result, value))
                {
                    return;
                }

                RaisePropertyChanged(nameof(HasResult));
                RaiseCommandState();
            }
        }

        public DelegateCommand<ImportDriveItemViewModel> SelectDriveCommand { get; }

        public DelegateCommand SelectSourceCommand { get; }

        public ImportSourceViewModel Source { get; }

        public DelegateCommand StartImportCommand { get; }

        public DelegateCommand StartNewImportCommand { get; }

        public int TotalFiles
        {
            get => _totalFiles;
            private set
            {
                if (!SetProperty(ref _totalFiles, value))
                {
                    return;
                }

                RaisePropertyChanged(nameof(IsProgressIndeterminate));
                RaisePropertyChanged(nameof(DiscoveredFiles));
                RaisePropertyChanged(nameof(ImportProgressText));
            }
        }

        /// <inheritdoc/>
        public bool CanCloseDialog() => !IsImporting;

        /// <inheritdoc/>
        public override void Destroy()
        {
            _discoveryTimer.Stop();
            _discoveryTimer.Tick -= OnDiscoveryTimerTick;
            AvailableDrives.CollectionChanged -= OnAvailableDrivesCollectionChanged;
            ReleaseDriveRefreshCancellation(cancel: true);
            ReleaseImportCancellation(cancel: true);
            ReleaseNetworkPathCancellation(cancel: true);
            base.Destroy();
        }

        /// <inheritdoc/>
        public void OnDialogClosed()
        {
            _discoveryTimer.Stop();
            ReleaseDriveRefreshCancellation(cancel: true);
            ReleaseImportCancellation(cancel: true);
            ReleaseNetworkPathCancellation(cancel: true);
        }

        /// <inheritdoc/>
        public void OnDialogOpened(IDialogParameters parameters)
        {
            IsCancellationRequested = false;
            IsInspectingSource = false;
            ImportProgressPercentage = 0d;
            ProcessedFiles = 0;
            TotalFiles = 0;
            Result = null;
            NetworkPath = string.Empty;
            NetworkPathStatusText = string.Empty;
            Source.Clear();
            RaiseCommandState();

            ExecuteRefreshDrives();
        }

        private bool CanCancelImport() => IsImporting && !IsCancellationRequested;

        private bool CanCheckNetworkPath() => !IsImporting && !IsCheckingNetworkPath && !string.IsNullOrWhiteSpace(NetworkPath);

        private bool CanRefreshDrives() => !IsImporting;

        private bool CanSelectDrive(ImportDriveItemViewModel? drive) => CanSelectSource() && drive?.IsReady == true;

        private bool CanSelectSource() => !IsImporting;

        private bool CanStartImport() => !IsImporting && Source.HasSelection;

        private bool CanStartNewImport() => !IsImporting && HasResult;

        private void CancelImport()
        {
            if (!CanCancelImport())
            {
                return;
            }

            /*
             * Reflect the request in the UI immediately. A synchronous operating-system
             * file-system call may finish before the worker can observe cancellation.
             */
            IsCancellationRequested = true;
            _importCancellation?.Cancel();
        }

        private void CloseDialog()
        {
            if (!CanCloseDialog())
            {
                return;
            }

            RequestClose.Invoke(ButtonResult.Cancel);
        }

        private async void ExecuteCheckNetworkPath()
        {
            try
            {
                await CheckNetworkPathAsync();
            }
            catch (OperationCanceledException)
            {
                // Network-path cancellation is expected when the user checks another path or closes the dialog.
            }
            catch (Exception)
            {
                NetworkPathStatusText = Properties.Resources.NetworkPathCheckFailed;
            }
        }

        private async void ExecuteRefreshDrives()
        {
            try
            {
                await RefreshDrivesAsync();
            }
            catch (OperationCanceledException)
            {
                // Refresh cancellation is expected when the user refreshes again or closes the dialog.
            }
            catch (Exception exception)
            {
                /*
                 * Unexpected drive-enumeration failures are converted into the existing
                 * structured result so technical details do not escape through the WPF surface.
                 */
                var resultViewModel = new ImportResultViewModel();
                resultViewModel.Apply(DicomImportResult.Failed(exception));
                Result = resultViewModel;
            }
        }

        private async void ExecuteStartImport()
        {
            try
            {
                await StartImportAsync();
            }
            catch (Exception exception)
            {
                /*
                 * Unexpected UI-boundary failures are converted into the structured
                 * result so technical details never escape through the WPF surface.
                 */
                var resultViewModel = new ImportResultViewModel();
                resultViewModel.Apply(DicomImportResult.Failed(exception));
                Result = resultViewModel;
            }
        }

        private void OnAvailableDrivesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            RaisePropertyChanged(nameof(DriveSearchFoundCountText));

        private void OnDiscoveryTimerTick(object? sender, EventArgs e) => DiscoveryElapsedSeconds++;

        private void RaiseCommandState()
        {
            CancelImportCommand.RaiseCanExecuteChanged();
            CheckNetworkPathCommand.RaiseCanExecuteChanged();
            CloseDialogCommand.RaiseCanExecuteChanged();
            RefreshDrivesCommand.RaiseCanExecuteChanged();
            SelectDriveCommand.RaiseCanExecuteChanged();
            SelectSourceCommand.RaiseCanExecuteChanged();
            StartImportCommand.RaiseCanExecuteChanged();
            StartNewImportCommand.RaiseCanExecuteChanged();
        }

        private async Task CheckNetworkPathAsync()
        {
            if (!CanCheckNetworkPath())
            {
                return;
            }

            ReleaseNetworkPathCancellation(cancel: true);
            _networkPathCancellation = new CancellationTokenSource();
            var cancellationToken = _networkPathCancellation.Token;
            var networkPath = NetworkPath.Trim();

            IsCheckingNetworkPath = true;
            NetworkPathStatusText = Properties.Resources.NetworkPathChecking;

            try
            {
                var resolution = await _importService.ResolveSourceAsync(new DicomImportRequest(networkPath, DicomImportSourceType.NetworkShare), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (resolution.Exists && resolution.IsReady && resolution.IsRepositoryImportSupported)
                {
                    Source.Select(resolution.EffectiveSourcePath);
                    NetworkPathStatusText = Properties.Resources.NetworkPathAvailable;
                    Result = null;
                    RaiseCommandState();
                    return;
                }

                NetworkPathStatusText = Properties.Resources.NetworkPathUnavailable;
            }
            finally
            {
                if (_networkPathCancellation?.Token == cancellationToken)
                {
                    IsCheckingNetworkPath = false;
                    ReleaseNetworkPathCancellation(cancel: false);
                }
            }
        }

        private async Task RefreshDrivesAsync()
        {
            if (!CanRefreshDrives())
            {
                return;
            }

            /*
             * A new refresh supersedes the previous enumeration. Only one operating-system
             * scan may update the collection at a time, preventing stale results from a
             * cancelled scan from reappearing after the user starts a new scan.
             */
            ReleaseDriveRefreshCancellation(cancel: true);
            _driveRefreshCancellation = new CancellationTokenSource();

            var cancellationToken = _driveRefreshCancellation.Token;

            AvailableDrives.Clear();
            IsRefreshingDrives = true;

            try
            {
                await foreach (var drive in _dicomImportDriveProvider.GetDrivesAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    /*
                     * The provider yields each inspected drive separately. Adding the item
                     * immediately keeps the user informed and allows ready sources to be
                     * selected while slower network or optical drives are still inspected.
                     */
                    AvailableDrives.Add(new ImportDriveItemViewModel(drive));
                    SelectDriveCommand.RaiseCanExecuteChanged();
                }
            }
            finally
            {
                /*
                 * A superseded scan must not reset the state of its replacement scan.
                 * The token comparison ensures that only the current enumeration owns
                 * the visible refresh state and its cancellation source.
                 */
                if (_driveRefreshCancellation?.Token == cancellationToken)
                {
                    IsRefreshingDrives = false;
                    ReleaseDriveRefreshCancellation(cancel: false);
                }
            }
        }

        private void ReleaseDriveRefreshCancellation(bool cancel)
        {
            var cancellation = _driveRefreshCancellation;
            _driveRefreshCancellation = null;

            if (cancellation is null)
            {
                return;
            }

            if (cancel)
            {
                cancellation.Cancel();
            }

            cancellation.Dispose();
        }

        private void ReleaseNetworkPathCancellation(bool cancel)
        {
            var cancellation = _networkPathCancellation;
            _networkPathCancellation = null;

            if (cancellation is null)
            {
                return;
            }

            if (cancel)
            {
                cancellation.Cancel();
            }

            cancellation.Dispose();
        }

        private void ReleaseImportCancellation(bool cancel)
        {
            var cancellation = _importCancellation;
            _importCancellation = null;

            if (cancellation is null)
            {
                return;
            }

            if (cancel)
            {
                cancellation.Cancel();
            }

            cancellation.Dispose();
        }

        private void SelectDrive(ImportDriveItemViewModel? drive)
        {
            if (!CanSelectDrive(drive))
            {
                return;
            }

            Source.Select(drive!.RootPath);
            IsCancellationRequested = false;
            Result = null;
            RaiseCommandState();
        }

        private void SelectSource()
        {
            var selectedPath = _fileDialogService.SelectFolder(Properties.Resources.SelectSourceDialogTitle, Source.HasSelection ? Source.Path : null);

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            Source.Select(selectedPath);
            IsCancellationRequested = false;
            Result = null;
            RaiseCommandState();
        }

        private async Task StartImportAsync()
        {
            if (!CanStartImport())
            {
                return;
            }

            /*
             * Drive enumeration is no longer needed after the user starts the import.
             * Cancelling it avoids unnecessary access to slow or unavailable media while
             * repository and DICOM processing are active.
             */
            ReleaseDriveRefreshCancellation(cancel: true);
            ReleaseNetworkPathCancellation(cancel: true);
            IsRefreshingDrives = false;

            ReleaseImportCancellation(cancel: false);
            _importCancellation = new CancellationTokenSource();

            IsImporting = true;
            IsCancellationRequested = false;
            IsInspectingSource = true;
            ImportProgressPercentage = 0d;
            ProcessedFiles = 0;
            TotalFiles = 0;
            DiscoveryElapsedSeconds = 0;
            _discoveryTimer.Start();
            Result = null;

            /*
             * Complete the current dispatcher cycle before starting DICOM or file-system
             * work. This guarantees that the progress section and cancellation command
             * become visible and interactive before the worker operation begins.
             */
            await Dispatcher.Yield(DispatcherPriority.Background);

            try
            {
                var request = new DicomImportRequest(
                    Source.Path,
                    Source.SourceType);

                /*
                 * Progress<T> is created on the WPF dispatcher thread. Progress reports
                 * are therefore marshalled safely back to the dispatcher.
                 */
                var progress = new Progress<DicomImportProgress>(UpdateProgress);
                var cancellationToken = _importCancellation.Token;

                /*
                 * DICOM parsing and repository processing contain synchronous CPU and
                 * file-system operations. A dedicated worker prevents those operations
                 * from occupying the WPF dispatcher.
                 */
                var result = await Task.Factory.StartNew(() => _dicomImportService.ImportAsync(request, progress, cancellationToken), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

                var resultViewModel = new ImportResultViewModel();
                resultViewModel.Apply(result);
                Result = resultViewModel;
            }
            finally
            {
                _discoveryTimer.Stop();
                IsImporting = false;
                ReleaseImportCancellation(cancel: false);
            }
        }

        private void StartNewImport()
        {
            if (IsImporting)
            {
                return;
            }

            IsCancellationRequested = false;
            IsInspectingSource = false;
            ImportProgressPercentage = 0d;
            ProcessedFiles = 0;
            TotalFiles = 0;
            Result = null;
            NetworkPath = string.Empty;
            NetworkPathStatusText = string.Empty;
            Source.Clear();
            RaiseCommandState();
            ExecuteRefreshDrives();
        }

        private void UpdateProgress(DicomImportProgress progress)
        {
            ArgumentNullException.ThrowIfNull(progress);

            if (IsCancellationRequested)
            {
                return;
            }

            if (progress.ProcessedFiles > 0 || progress.TotalFiles > 0)
            {
                _discoveryTimer.Stop();
            }

            TotalFiles = progress.TotalFiles;
            ProcessedFiles = progress.ProcessedFiles;
            IsInspectingSource = progress.IsInspectingSource;
            ImportProgressPercentage = progress.Percentage;

            RaisePropertyChanged(nameof(IsProgressIndeterminate));
            RaisePropertyChanged(nameof(DiscoveredFiles));
            RaisePropertyChanged(nameof(ImportProgressText));
        }
    }
}