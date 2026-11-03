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
        private bool _isImporting;
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
        public DelegateCommand CancelImportCommand { get; }

        public DelegateCommand CloseDialogCommand { get; }

        public int DiscoveredFiles => IsProgressIndeterminate ? ProcessedFiles : TotalFiles;
        public bool HasResult => Result is not null;

        public double ImportProgressPercentage
        {
            get => _importProgressPercentage;
            private set => SetProperty(ref _importProgressPercentage, value);
        }

        public string ImportProgressText => IsProgressIndeterminate ? string.Format(Properties.Resources.ImportProgressDiscoveringFormat, DiscoveredFiles) : string.Format(Properties.Resources.ImportProgressProcessingFormat, ProcessedFiles, TotalFiles);

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

        public bool IsProgressIndeterminate => TotalFiles <= 0;

        public int ProcessedFiles
        {
            get => _processedFiles;
            private set
            {
                if (!SetProperty(ref _processedFiles, value)) return;

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
                if (!SetProperty(ref _totalFiles, value)) return;

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
            Result = null;
            Source.Clear();
            RaiseCommandState();
        }

        private bool CanCancelImport() => IsImporting;

        private void CancelImport() => _importCancellation?.Cancel();

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
                // Unexpected UI-boundary failures are converted into the existing
                // structured result so technical details never escape through WPF.
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
            Result = null;
            RaiseCommandState();
        }
        private async Task StartImportAsync()
        {
            if (!CanStartImport()) return;

            ReleaseImportCancellation(cancel: false);
            _importCancellation = new CancellationTokenSource();

            IsImporting = true;
            Result = null;
            ImportProgressPercentage = 0d;
            ProcessedFiles = 0;
            TotalFiles = 0;

            /*
             * Complete the current dispatcher cycle before starting any DICOM or file-system
             * work. This guarantees that the progress section and cancellation command become
             * visible and interactive before the worker operation begins.
             */
            await Dispatcher.Yield(DispatcherPriority.Background);

            try
            {
                var request = new DicomImportRequest(Source.Path, Source.SourceType);
                var progress = new Progress<DicomImportProgress>(UpdateProgress);
                var cancellationToken = _importCancellation.Token;

                /*
                 * DICOM parsing and repository processing contain synchronous CPU and file-system
                 * operations. A dedicated long-running worker prevents those operations from
                 * occupying the WPF dispatcher or depending on immediate ThreadPool availability.
                 */
                var result = await Task.Factory.StartNew(
                    () => _dicomImportService.ImportAsync(request, progress, cancellationToken),
                    cancellationToken,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default).Unwrap();

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

            Result = null;
            Source.Clear();
            RaiseCommandState();
        }

        private void UpdateProgress(DicomImportProgress progress)
        {
            ArgumentNullException.ThrowIfNull(progress);
            TotalFiles = progress.TotalFiles;
            ProcessedFiles = progress.ProcessedFiles;
            ImportProgressPercentage = progress.Percentage;
        }
    }
}