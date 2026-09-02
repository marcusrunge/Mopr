using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System;
using System.Threading;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels
{
    /// <summary>
    /// Provides database and repository-location validation for the machine-wide MOPR setup.
    /// </summary>
    public sealed class SetupViewModel(IMachineConfigurationService configurationService, IRepositoryLocationValidationService repositoryLocationValidationService, IWpf wpf) : BindableBase, INavigationAware
    {
        private const int DatabaseStep = 1;
        private const int RepositoryStep = 2;
        private const int VerificationStep = 3;
        private const int CompletionStep = 4;

        private readonly IMachineConfigurationService _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        private readonly IRepositoryLocationValidationService _repositoryLocationValidationService = repositoryLocationValidationService ?? throw new ArgumentNullException(nameof(repositoryLocationValidationService));
        private readonly IWpf _wpf = wpf ?? throw new ArgumentNullException(nameof(wpf));
        private CancellationTokenSource? _databaseTestCancellation;
        private CancellationTokenSource? _repositoryValidationCancellation;
        private DelegateCommand? _backCommand, _continueCommand, _selectRepositoryLocationCommand, _testDatabaseConnectionCommand, _validateRepositoryLocationCommand;
        private string _connectionString = string.Empty, _databaseStatusText = string.Empty, _repositoryLocationPath = string.Empty, _repositoryStatusText = string.Empty;
        private bool? _isDatabaseConnectionSuccessful, _isRepositoryLocationValid;
        private bool _isLoading, _isSetupComplete, _isTestingDatabase, _isValidatingRepositoryLocation;
        private int _currentStep = DatabaseStep;
        /// <summary>
        /// Gets a value indicating whether the completion step is active.
        /// </summary>
        public bool IsCompletionStep => CurrentStep == CompletionStep;

        public bool CanModify => _configurationService.CanModify;
        public bool CanContinueFromDatabase => IsDatabaseConnectionSuccessful == true && !IsLoading && !IsTestingDatabase;
        public bool CanContinueFromRepository => IsRepositoryLocationValid == true && !IsLoading && !IsValidatingRepositoryLocation;

        public string ConnectionString
        {
            get => _connectionString;
            set
            {
                if (!SetProperty(ref _connectionString, value)) return;
                IsDatabaseConnectionSuccessful = null;
                DatabaseStatusText = string.Empty;
                RaiseCommandStates();
            }
        }

        public int CurrentStep
        {
            get => _currentStep;
            private set
            {
                if (!SetProperty(ref _currentStep, value)) return;

                RaisePropertyChanged(nameof(IsDatabaseStep));
                RaisePropertyChanged(nameof(IsRepositoryStep));
                RaisePropertyChanged(nameof(IsVerificationStep));
                RaisePropertyChanged(nameof(IsCompletionStep));
                RaiseCommandStates();
            }
        }
        public string DatabaseStatusText { get => _databaseStatusText; private set => SetProperty(ref _databaseStatusText, value); }
        public bool IsDatabaseStep => CurrentStep == DatabaseStep;

        public bool? IsDatabaseConnectionSuccessful
        {
            get => _isDatabaseConnectionSuccessful;
            private set
            {
                if (SetProperty(ref _isDatabaseConnectionSuccessful, value)) RaiseCommandStates();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value)) RaiseCommandStates();
            }
        }

        public bool IsRepositoryStep => CurrentStep == RepositoryStep;

        public bool? IsRepositoryLocationValid
        {
            get => _isRepositoryLocationValid;
            private set
            {
                if (SetProperty(ref _isRepositoryLocationValid, value)) RaiseCommandStates();
            }
        }

        public bool IsSetupComplete { get => _isSetupComplete; private set => SetProperty(ref _isSetupComplete, value); }

        public bool IsTestingDatabase
        {
            get => _isTestingDatabase;
            private set
            {
                if (SetProperty(ref _isTestingDatabase, value)) RaiseCommandStates();
            }
        }

        public bool IsValidatingRepositoryLocation
        {
            get => _isValidatingRepositoryLocation;
            private set
            {
                if (SetProperty(ref _isValidatingRepositoryLocation, value)) RaiseCommandStates();
            }
        }

        public bool IsVerificationStep => CurrentStep == VerificationStep;

        public string RepositoryLocationPath
        {
            get => _repositoryLocationPath;
            set
            {
                if (!SetProperty(ref _repositoryLocationPath, value)) return;
                IsRepositoryLocationValid = null;
                RepositoryStatusText = string.Empty;
                RaiseCommandStates();
            }
        }

        public string RepositoryStatusText { get => _repositoryStatusText; private set => SetProperty(ref _repositoryStatusText, value); }
        public DelegateCommand BackCommand => _backCommand ??= new DelegateCommand(ExecuteBack, CanExecuteBack);
        public DelegateCommand ContinueCommand => _continueCommand ??= new DelegateCommand(ExecuteContinue, CanExecuteContinue);
        public DelegateCommand SelectRepositoryLocationCommand => _selectRepositoryLocationCommand ??= new DelegateCommand(ExecuteSelectRepositoryLocation, CanSelectRepositoryLocation);
        public DelegateCommand TestDatabaseConnectionCommand => _testDatabaseConnectionCommand ??= new DelegateCommand(ExecuteTestDatabaseConnection, CanTestDatabaseConnection);
        public DelegateCommand ValidateRepositoryLocationCommand => _validateRepositoryLocationCommand ??= new DelegateCommand(ExecuteValidateRepositoryLocation, CanValidateRepositoryLocation);

        /// <inheritdoc/>
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <inheritdoc/>
        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            try
            {
                IsLoading = true;
                DatabaseStatusText = string.Empty;
                IsDatabaseConnectionSuccessful = null;
                RepositoryStatusText = string.Empty;
                IsRepositoryLocationValid = null;
                var configuration = await _configurationService.LoadAsync().ConfigureAwait(true);
                ConnectionString = configuration.Database.ConnectionString;
                IsSetupComplete = configuration.IsSetupComplete;
                RaisePropertyChanged(nameof(CanModify));
            }
            catch (OperationCanceledException)
            {
                DatabaseStatusText = Resources.Setup_ConfigurationLoadCanceled;
            }
            catch
            {
                // Technical details remain outside the user-facing setup state.
                DatabaseStatusText = Resources.Setup_ConfigurationLoadFailed;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <inheritdoc/>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            CancelAndDispose(ref _databaseTestCancellation);
            CancelAndDispose(ref _repositoryValidationCancellation);
        }

        private bool CanExecuteBack() => CurrentStep > DatabaseStep && !IsLoading && !IsTestingDatabase && !IsValidatingRepositoryLocation;
        private bool CanExecuteContinue() => IsDatabaseStep ? CanContinueFromDatabase : IsRepositoryStep ? CanContinueFromRepository : IsVerificationStep;
        private bool CanSelectRepositoryLocation() => CanModify && IsRepositoryStep && !IsLoading && !IsValidatingRepositoryLocation;
        private bool CanTestDatabaseConnection() => CanModify && IsDatabaseStep && !IsLoading && !IsTestingDatabase && !string.IsNullOrWhiteSpace(ConnectionString);
        private bool CanValidateRepositoryLocation() => CanModify && IsRepositoryStep && !IsLoading && !IsValidatingRepositoryLocation && !string.IsNullOrWhiteSpace(RepositoryLocationPath);

        private static void CancelAndDispose(ref CancellationTokenSource? cancellation)
        {
            if (cancellation is null) return;
            cancellation.Cancel();
            cancellation.Dispose();
            cancellation = null;
        }

        private void ExecuteBack()
        {
            if (IsCompletionStep) CurrentStep = VerificationStep;
            else if (IsVerificationStep) CurrentStep = RepositoryStep;
            else if (IsRepositoryStep) CurrentStep = DatabaseStep;
        }

        private void ExecuteContinue()
        {
            if (IsDatabaseStep && CanContinueFromDatabase) CurrentStep = RepositoryStep;
            else if (IsRepositoryStep && CanContinueFromRepository) CurrentStep = VerificationStep;
            else if (IsVerificationStep) CurrentStep = CompletionStep;
        }

        private void ExecuteSelectRepositoryLocation()
        {
            var fileDialogService = _wpf.DialogService?.FileDialogService ?? throw new InvalidOperationException("The WPF file dialog service has not been initialized.");
            var selectedPath = fileDialogService.SelectFolder(Resources.Setup_RepositoryFolderDialogTitle, string.IsNullOrWhiteSpace(RepositoryLocationPath) ? null : RepositoryLocationPath);
            if (!string.IsNullOrWhiteSpace(selectedPath)) RepositoryLocationPath = selectedPath;
        }

        private async void ExecuteTestDatabaseConnection()
        {
            CancelAndDispose(ref _databaseTestCancellation);
            _databaseTestCancellation = new CancellationTokenSource();
            try
            {
                IsTestingDatabase = true;
                IsDatabaseConnectionSuccessful = null;
                DatabaseStatusText = Resources.Setup_DatabaseConnectionTesting;
                var isSuccessful = await _configurationService.TestDatabaseConnectionAsync(new SetupDatabaseConfiguration(ConnectionString), _databaseTestCancellation.Token).ConfigureAwait(true);
                IsDatabaseConnectionSuccessful = isSuccessful;
                DatabaseStatusText = isSuccessful ? Resources.Setup_DatabaseConnectionSuccessful : Resources.Setup_DatabaseConnectionFailed;
            }
            catch (OperationCanceledException)
            {
                IsDatabaseConnectionSuccessful = null;
                DatabaseStatusText = Resources.Setup_DatabaseConnectionCanceled;
            }
            catch
            {
                // The UI receives a localized status while technical diagnostics remain in the service boundary.
                IsDatabaseConnectionSuccessful = false;
                DatabaseStatusText = Resources.Setup_DatabaseConnectionFailed;
            }
            finally
            {
                IsTestingDatabase = false;
                CancelAndDispose(ref _databaseTestCancellation);
            }
        }

        private async void ExecuteValidateRepositoryLocation()
        {
            CancelAndDispose(ref _repositoryValidationCancellation);
            _repositoryValidationCancellation = new CancellationTokenSource();
            try
            {
                IsValidatingRepositoryLocation = true;
                IsRepositoryLocationValid = null;
                RepositoryStatusText = Resources.Setup_RepositoryValidationRunning;
                var result = await _repositoryLocationValidationService.ValidateAsync(RepositoryLocationPath, _repositoryValidationCancellation.Token).ConfigureAwait(true);
                ApplyRepositoryValidationResult(result);
            }
            catch (OperationCanceledException)
            {
                IsRepositoryLocationValid = null;
                RepositoryStatusText = Resources.Setup_RepositoryValidationCanceled;
            }
            catch
            {
                // Technical file-system details remain outside the user-facing setup state.
                IsRepositoryLocationValid = false;
                RepositoryStatusText = Resources.Setup_RepositoryValidationFailed;
            }
            finally
            {
                IsValidatingRepositoryLocation = false;
                CancelAndDispose(ref _repositoryValidationCancellation);
            }
        }

        private void ApplyRepositoryValidationResult(RepositoryLocationValidationResult result)
        {
            if (result.IsValid)
            {
                RepositoryLocationPath = result.NormalizedPath ?? RepositoryLocationPath;
                IsRepositoryLocationValid = true;
                RepositoryStatusText = Resources.Setup_RepositoryValidationSuccessful;
                return;
            }

            IsRepositoryLocationValid = false;
            RepositoryStatusText = !result.Exists ? Resources.Setup_RepositoryDirectoryMissing : !result.IsReadable ? Resources.Setup_RepositoryDirectoryNotReadable : !result.IsWritable ? Resources.Setup_RepositoryDirectoryNotWritable : Resources.Setup_RepositoryValidationFailed;
        }

        private void RaiseCommandStates()
        {
            RaisePropertyChanged(nameof(CanContinueFromDatabase));
            RaisePropertyChanged(nameof(CanContinueFromRepository));
            _backCommand?.RaiseCanExecuteChanged();
            _continueCommand?.RaiseCanExecuteChanged();
            _selectRepositoryLocationCommand?.RaiseCanExecuteChanged();
            _testDatabaseConnectionCommand?.RaiseCanExecuteChanged();
            _validateRepositoryLocationCommand?.RaiseCanExecuteChanged();
        }

        private sealed class SetupDatabaseConfiguration(string connectionString) : IDatabaseConfiguration
        {
            /// <inheritdoc/>
            public string ConnectionString { get; } = connectionString;
        }
    }
}
