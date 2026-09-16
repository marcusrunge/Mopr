using MarcusRunge.Mopr.Workbench.Contracts.Application.Import.Models;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;

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
        private bool _isImporting;
        private ImportResultViewModel? _result;

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

        public DelegateCommand CancelImportCommand { get; }

        public DelegateCommand CloseDialogCommand { get; }

        public bool HasResult => Result is not null;

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

        /// <inheritdoc/>
        public DialogCloseListener RequestClose { get; }

        public DelegateCommand SelectSourceCommand { get; }

        public ImportSourceViewModel Source { get; }

        public DelegateCommand StartImportCommand { get; }

        public DelegateCommand StartNewImportCommand { get; }

        public string Title => Properties.Resources.ImportTitle;

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

        private bool CanSelectSource() => !IsImporting;

        private bool CanStartImport() => !IsImporting && Source.HasSelection;

        private bool CanStartNewImport() => !IsImporting && HasResult;

        private void CancelImport() => _importCancellation?.Cancel();

        private void CloseDialog()
        {
            if (!CanCloseDialog())
            {
                return;
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
            if (!CanStartImport())
            {
                return;
            }

            ReleaseImportCancellation(cancel: false);
            _importCancellation = new CancellationTokenSource();

            IsImporting = true;
            Result = null;

            try
            {
                var request = new DicomImportRequest(Source.Path, Source.SourceType);
                var result = await _dicomImportService.ImportAsync(request, _importCancellation.Token);
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
    }
}