using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Import;
using System.Windows.Threading;

namespace MarcusRunge.Mopr.Workbench.Modules.Import.ViewModels
{
    /// <summary>
    /// Coordinates source selection and user-initiated DICOM import operations.
    /// </summary>
    public sealed class ImportViewModel : ViewModelBase, IDialogAware
    {
        private readonly IDicomImportService _dicomImportService;
        private readonly IFileDialogService _fileDialogService;
        private CancellationTokenSource? _importCancellation;
        private double _importProgressPercentage;
        private bool _isCancellationRequested;
        private bool _isImporting;
        private bool _isInspectingSource;
        private int _processedFiles;
        private ImportResultViewModel? _result;
        private int _totalFiles;

        public ImportViewModel(IApplication application)
        {
            ArgumentNullException.ThrowIfNull(application);

            _fileDialogService = application.DialogService?.FileDialogService ?? throw new InvalidOperationException("The file dialog service has not been initialized.");

            _dicomImportService = application.ImportService?.DicomImportService ?? throw new InvalidOperationException("The DICOM import service has not been initialized.");

            Source = new ImportSourceViewModel();

            SelectSourceCommand = new DelegateCommand(SelectSource, CanSelectSource);
            StartImportCommand = new DelegateCommand(ExecuteStartImport, CanStartImport);
            CancelImportCommand = new DelegateCommand(CancelImport, CanCancelImport);
            StartNewImportCommand = new DelegateCommand(StartNewImport, CanStartNewImport);
            CloseDialogCommand = new DelegateCommand(CloseDialog, CanCloseDialog);
        }

        public static string Title => Properties.Resources.ImportTitle;

        public string CancelImportButtonText => IsCancellationRequested ? Properties.Resources.ImportCancellationRequestedButton : Properties.Resources.CancelImportButton;

        public DelegateCommand CancelImportCommand { get; }

        public DelegateCommand CloseDialogCommand { get; }

        public int DiscoveredFiles => IsProgressIndeterminate ? ProcessedFiles : TotalFiles;

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

                /*
                 * The final number remains unknown while the directory tree is being
                 * enumerated. ProcessedFiles contains the number of paths returned by
                 * the file system so far.
                 */
                if (TotalFiles <= 0)
                {
                    return ProcessedFiles <= 0 ? Properties.Resources.ImportProgressDiscovering : string.Format(Properties.Resources.ImportProgressDiscoveredFormat, ProcessedFiles);
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
            ReleaseImportCancellation(cancel: true);
            base.Destroy();
        }

        /// <inheritdoc/>
        public void OnDialogClosed() => ReleaseImportCancellation(cancel: true);

        /// <inheritdoc/>
        public void OnDialogOpened(IDialogParameters parameters)
        {
            IsCancellationRequested = false;
            IsInspectingSource = false;
            ImportProgressPercentage = 0d;
            ProcessedFiles = 0;
            TotalFiles = 0;
            Result = null;
            Source.Clear();
            RaiseCommandState();
        }

        private bool CanCancelImport() => IsImporting && !IsCancellationRequested;

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

        private bool CanSelectSource() => !IsImporting;

        private bool CanStartImport() => !IsImporting && Source.HasSelection;

        private bool CanStartNewImport() => !IsImporting && HasResult;

        private void CloseDialog()
        {
            if (!CanCloseDialog())
            {
                return;
            }

            RequestClose.Invoke(ButtonResult.Cancel);
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

        private void RaiseCommandState()
        {
            CancelImportCommand.RaiseCanExecuteChanged();
            CloseDialogCommand.RaiseCanExecuteChanged();
            SelectSourceCommand.RaiseCanExecuteChanged();
            StartImportCommand.RaiseCanExecuteChanged();
            StartNewImportCommand.RaiseCanExecuteChanged();
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

            ReleaseImportCancellation(cancel: false);
            _importCancellation = new CancellationTokenSource();

            IsImporting = true;
            IsCancellationRequested = false;
            IsInspectingSource = true;
            ImportProgressPercentage = 0d;
            ProcessedFiles = 0;
            TotalFiles = 0;
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
            Source.Clear();
            RaiseCommandState();
        }

        private void UpdateProgress(DicomImportProgress progress)
        {
            ArgumentNullException.ThrowIfNull(progress);

            /*
             * Ignore late worker reports after cancellation was requested. The UI must
             * continue to display the cancellation state instead of reverting to an
             * earlier discovery or processing message.
             */
            if (IsCancellationRequested)
            {
                return;
            }

            TotalFiles = progress.TotalFiles;
            ProcessedFiles = progress.ProcessedFiles;
            IsInspectingSource = progress.IsInspectingSource;
            ImportProgressPercentage = progress.Percentage;

            /*
             * The displayed text depends on the complete progress snapshot. Raise the
             * dependent properties after all values have been assigned.
             */
            RaisePropertyChanged(nameof(IsProgressIndeterminate));
            RaisePropertyChanged(nameof(DiscoveredFiles));
            RaisePropertyChanged(nameof(ImportProgressText));
        }
    }
}