using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using MarcusRunge.Mopr.Workbench.Core;
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
    /// Coordinates the machine-wide MOPR setup workflow.
    /// </summary>
    public sealed class SetupViewModel : BindableBase, INavigationAware, IConfirmNavigationRequest
    {
        private const int DatabaseStep = 1;
        private const int RepositoryStep = 2;
        private const int VerificationStep = 3;
        private const int CompletionStep = 4;

        private readonly IMachineConfigurationService _configurationService;
        private readonly IRegionManager _regionManager;
        private readonly IRepositoryLocationValidationService _repositoryLocationValidationService;
        private readonly ISetupCompletionService _setupCompletionService;
        private readonly IWpf _wpf;

        private IApplicationConfiguration? _applicationConfiguration;
        private DelegateCommand? _backCommand;
        private DelegateCommand? _cancelSetupCompletionCommand;
        private DelegateCommand? _completeSetupCommand;
        private DelegateCommand? _continueCommand;
        private DelegateCommand? _selectRepositoryLocationCommand;
        private DelegateCommand? _testDatabaseConnectionCommand;
        private DelegateCommand? _validateRepositoryLocationCommand;
        private CancellationTokenSource? _databaseTestCancellation;
        private CancellationTokenSource? _repositoryValidationCancellation;
        private CancellationTokenSource? _setupCompletionCancellation;
        private string _completionStatusText = string.Empty;
        private string _connectionString = string.Empty;
        private string _databaseStatusText = string.Empty;
        private string _repositoryLocationPath = string.Empty;
        private string _repositoryStatusText = string.Empty;
        private bool? _isDatabaseConnectionSuccessful;
        private bool? _isRepositoryLocationValid;
        private bool _isCompletingSetup;
        private bool _isLoading;
        private bool _isSetupComplete;
        private bool _isTestingDatabase;
        private bool _isValidatingRepositoryLocation;
        private int _currentStep = DatabaseStep;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetupViewModel"/> class.
        /// </summary>
        /// <param name="configurationService">The machine-wide configuration service.</param>
        /// <param name="repositoryLocationValidationService">The repository-location validation service.</param>
        /// <param name="setupCompletionService">The setup-completion service.</param>
        /// <param name="wpf">The WPF service facade.</param>
        /// <param name="regionManager">The Prism region manager.</param>
        public SetupViewModel(
            IMachineConfigurationService configurationService,
            IRepositoryLocationValidationService repositoryLocationValidationService,
            ISetupCompletionService setupCompletionService,
            IWpf wpf,
            IRegionManager regionManager)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _repositoryLocationValidationService = repositoryLocationValidationService ?? throw new ArgumentNullException(nameof(repositoryLocationValidationService));
            _setupCompletionService = setupCompletionService ?? throw new ArgumentNullException(nameof(setupCompletionService));
            _wpf = wpf ?? throw new ArgumentNullException(nameof(wpf));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        }

        /// <summary>
        /// Gets the command that returns to the preceding setup step.
        /// </summary>
        public DelegateCommand BackCommand => _backCommand ??= new DelegateCommand(ExecuteBack, CanExecuteBack);

        /// <summary>
        /// Gets a value indicating whether the current process may modify the machine-wide configuration.
        /// </summary>
        public bool CanModify => _configurationService.CanModify;

        /// <summary>
        /// Gets a value indicating whether the database step may be continued.
        /// </summary>
        public bool CanContinueFromDatabase => IsDatabaseConnectionSuccessful == true
            && !IsLoading
            && !IsTestingDatabase
            && !IsCompletingSetup;

        /// <summary>
        /// Gets a value indicating whether the repository step may be continued.
        /// </summary>
        public bool CanContinueFromRepository => IsRepositoryLocationValid == true
            && !IsLoading
            && !IsValidatingRepositoryLocation
            && !IsCompletingSetup;

        /// <summary>
        /// Gets the command that requests cancellation of an active setup-completion operation.
        /// </summary>
        public DelegateCommand CancelSetupCompletionCommand => _cancelSetupCompletionCommand ??=
            new DelegateCommand(ExecuteCancelSetupCompletion, CanExecuteCancelSetupCompletion);

        /// <summary>
        /// Gets the command that completes the machine-wide setup.
        /// </summary>
        public DelegateCommand CompleteSetupCommand => _completeSetupCommand ??=
            new DelegateCommand(ExecuteCompleteSetup, CanExecuteCompleteSetup);

        /// <summary>
        /// Gets the localized setup-completion status text.
        /// </summary>
        public string CompletionStatusText
        {
            get => _completionStatusText;
            private set => SetProperty(ref _completionStatusText, value);
        }

        /// <summary>
        /// Gets or sets the SQL Server connection string.
        /// </summary>
        public string ConnectionString
        {
            get => _connectionString;
            set
            {
                if (!SetProperty(ref _connectionString, value))
                {
                    return;
                }

                IsDatabaseConnectionSuccessful = null;
                DatabaseStatusText = string.Empty;
                CompletionStatusText = string.Empty;
                RaiseCommandStates();
            }
        }

        /// <summary>
        /// Gets the command that continues with the next setup step.
        /// </summary>
        public DelegateCommand ContinueCommand => _continueCommand ??=
            new DelegateCommand(ExecuteContinue, CanExecuteContinue);

        /// <summary>
        /// Gets the active setup step.
        /// </summary>
        public int CurrentStep
        {
            get => _currentStep;
            private set
            {
                if (!SetProperty(ref _currentStep, value))
                {
                    return;
                }

                RaisePropertyChanged(nameof(IsCompletionStep));
                RaisePropertyChanged(nameof(IsDatabaseStep));
                RaisePropertyChanged(nameof(IsRepositoryStep));
                RaisePropertyChanged(nameof(IsVerificationStep));
                RaiseCommandStates();
            }
        }

        /// <summary>
        /// Gets the localized database-validation status text.
        /// </summary>
        public string DatabaseStatusText
        {
            get => _databaseStatusText;
            private set => SetProperty(ref _databaseStatusText, value);
        }

        /// <summary>
        /// Gets a value indicating whether the completion step is active.
        /// </summary>
        public bool IsCompletionStep => CurrentStep == CompletionStep;

        /// <summary>
        /// Gets a value indicating whether setup completion is currently running.
        /// </summary>
        public bool IsCompletingSetup
        {
            get => _isCompletingSetup;
            private set
            {
                if (!SetProperty(ref _isCompletingSetup, value))
                {
                    return;
                }

                RaisePropertyChanged(nameof(IsSetupInteractionEnabled));
                RaiseCommandStates();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the database step is active.
        /// </summary>
        public bool IsDatabaseStep => CurrentStep == DatabaseStep;

        /// <summary>
        /// Gets the last database-connection validation state.
        /// </summary>
        public bool? IsDatabaseConnectionSuccessful
        {
            get => _isDatabaseConnectionSuccessful;
            private set
            {
                if (SetProperty(ref _isDatabaseConnectionSuccessful, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the machine-wide configuration is loading.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    RaisePropertyChanged(nameof(IsSetupInteractionEnabled));
                    RaiseCommandStates();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the repository step is active.
        /// </summary>
        public bool IsRepositoryStep => CurrentStep == RepositoryStep;

        /// <summary>
        /// Gets the last repository-location validation state.
        /// </summary>
        public bool? IsRepositoryLocationValid
        {
            get => _isRepositoryLocationValid;
            private set
            {
                if (SetProperty(ref _isRepositoryLocationValid, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the loaded machine configuration is marked as complete.
        /// </summary>
        public bool IsSetupComplete
        {
            get => _isSetupComplete;
            private set => SetProperty(ref _isSetupComplete, value);
        }

        /// <summary>
        /// Gets a value indicating whether regular setup interaction is enabled.
        /// </summary>
        public bool IsSetupInteractionEnabled => !IsLoading && !IsCompletingSetup;

        /// <summary>
        /// Gets a value indicating whether the database connection is currently being tested.
        /// </summary>
        public bool IsTestingDatabase
        {
            get => _isTestingDatabase;
            private set
            {
                if (SetProperty(ref _isTestingDatabase, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the repository location is currently being validated.
        /// </summary>
        public bool IsValidatingRepositoryLocation
        {
            get => _isValidatingRepositoryLocation;
            private set
            {
                if (SetProperty(ref _isValidatingRepositoryLocation, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the verification step is active.
        /// </summary>
        public bool IsVerificationStep => CurrentStep == VerificationStep;

        /// <summary>
        /// Gets or sets the selected DICOM repository location.
        /// </summary>
        public string RepositoryLocationPath
        {
            get => _repositoryLocationPath;
            set
            {
                if (!SetProperty(ref _repositoryLocationPath, value))
                {
                    return;
                }

                IsRepositoryLocationValid = null;
                RepositoryStatusText = string.Empty;
                CompletionStatusText = string.Empty;
                RaiseCommandStates();
            }
        }

        /// <summary>
        /// Gets the localized repository-validation status text.
        /// </summary>
        public string RepositoryStatusText
        {
            get => _repositoryStatusText;
            private set => SetProperty(ref _repositoryStatusText, value);
        }

        /// <summary>
        /// Gets the command that displays the repository-folder selection dialog.
        /// </summary>
        public DelegateCommand SelectRepositoryLocationCommand => _selectRepositoryLocationCommand ??=
            new DelegateCommand(ExecuteSelectRepositoryLocation, CanSelectRepositoryLocation);

        /// <summary>
        /// Gets the command that tests the configured database connection.
        /// </summary>
        public DelegateCommand TestDatabaseConnectionCommand => _testDatabaseConnectionCommand ??=
            new DelegateCommand(ExecuteTestDatabaseConnection, CanTestDatabaseConnection);

        /// <summary>
        /// Gets the command that validates the selected repository location.
        /// </summary>
        public DelegateCommand ValidateRepositoryLocationCommand => _validateRepositoryLocationCommand ??=
            new DelegateCommand(ExecuteValidateRepositoryLocation, CanValidateRepositoryLocation);

        /// <inheritdoc/>
        public void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
        {
            if (continuationCallback is null)
            {
                throw new ArgumentNullException(nameof(continuationCallback));
            }

            // Navigation is rejected while setup completion owns the technical
            // configuration transition and may still need to compensate changes.
            continuationCallback(!IsCompletingSetup);
        }

        /// <inheritdoc/>
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <inheritdoc/>
        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            try
            {
                IsLoading = true;
                CompletionStatusText = string.Empty;
                DatabaseStatusText = string.Empty;
                IsDatabaseConnectionSuccessful = null;
                RepositoryStatusText = string.Empty;
                IsRepositoryLocationValid = null;

                var configuration = await _configurationService.LoadAsync().ConfigureAwait(true);

                _applicationConfiguration = configuration;
                ConnectionString = configuration.Database.ConnectionString;
                IsSetupComplete = configuration.IsSetupComplete;
                RaisePropertyChanged(nameof(CanModify));
            }
            catch (OperationCanceledException)
            {
                _applicationConfiguration = null;
                DatabaseStatusText = Resources.Setup_ConfigurationLoadCanceled;
            }
            catch
            {
                // Technical details remain outside the user-facing setup state.
                _applicationConfiguration = null;
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
            CancelAndDispose(ref _setupCompletionCancellation);
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
            RepositoryStatusText = !result.Exists
                ? Resources.Setup_RepositoryDirectoryMissing
                : !result.IsReadable
                    ? Resources.Setup_RepositoryDirectoryNotReadable
                    : !result.IsWritable
                        ? Resources.Setup_RepositoryDirectoryNotWritable
                        : Resources.Setup_RepositoryValidationFailed;
        }

        private void ApplySetupCompletionResult(SetupCompletionResult result)
        {
            switch (result.Status)
            {
                case SetupCompletionStatus.Completed:
                    CompletionStatusText = Resources.Setup_CompletionSuccessful;
                    IsSetupComplete = true;
                    NavigateToImagingWorkbench();
                    break;

                case SetupCompletionStatus.DatabaseValidationFailed:
                    CompletionStatusText = Resources.Setup_CompletionDatabaseValidationFailed;
                    break;

                case SetupCompletionStatus.RepositoryValidationFailed:
                    CompletionStatusText = Resources.Setup_CompletionRepositoryValidationFailed;
                    break;

                case SetupCompletionStatus.Canceled:
                    CompletionStatusText = Resources.Setup_CompletionCanceled;
                    break;

                case SetupCompletionStatus.FailedAndRollbackFailed:
                    CompletionStatusText = Resources.Setup_CompletionRollbackFailed;
                    break;

                case SetupCompletionStatus.Failed:
                case SetupCompletionStatus.NotStarted:
                default:
                    CompletionStatusText = Resources.Setup_CompletionFailed;
                    break;
            }
        }

        private bool CanExecuteBack() => CurrentStep > DatabaseStep
            && !IsLoading
            && !IsTestingDatabase
            && !IsValidatingRepositoryLocation
            && !IsCompletingSetup;

        private bool CanExecuteCancelSetupCompletion() => IsCompletingSetup
            && _setupCompletionCancellation is not null
            && !_setupCompletionCancellation.IsCancellationRequested;

        private bool CanExecuteCompleteSetup() => CanModify
            && IsCompletionStep
            && !IsLoading
            && !IsTestingDatabase
            && !IsValidatingRepositoryLocation
            && !IsCompletingSetup
            && IsDatabaseConnectionSuccessful == true
            && IsRepositoryLocationValid == true
            && _applicationConfiguration is not null
            && !string.IsNullOrWhiteSpace(ConnectionString)
            && !string.IsNullOrWhiteSpace(RepositoryLocationPath);

        private bool CanExecuteContinue() => !IsCompletingSetup
            && (IsDatabaseStep
                ? CanContinueFromDatabase
                : IsRepositoryStep
                    ? CanContinueFromRepository
                    : IsVerificationStep);

        private bool CanSelectRepositoryLocation() => CanModify
            && IsRepositoryStep
            && !IsLoading
            && !IsValidatingRepositoryLocation
            && !IsCompletingSetup;

        private bool CanTestDatabaseConnection() => CanModify
            && IsDatabaseStep
            && !IsLoading
            && !IsTestingDatabase
            && !IsCompletingSetup
            && !string.IsNullOrWhiteSpace(ConnectionString);

        private bool CanValidateRepositoryLocation() => CanModify
            && IsRepositoryStep
            && !IsLoading
            && !IsValidatingRepositoryLocation
            && !IsCompletingSetup
            && !string.IsNullOrWhiteSpace(RepositoryLocationPath);

        private static void CancelAndDispose(ref CancellationTokenSource? cancellation)
        {
            if (cancellation is null)
            {
                return;
            }

            cancellation.Cancel();
            cancellation.Dispose();
            cancellation = null;
        }

        private void CompleteOperation(
            ref CancellationTokenSource? field,
            CancellationTokenSource operationCancellation)
        {
            if (!ReferenceEquals(field, operationCancellation))
            {
                return;
            }

            operationCancellation.Dispose();
            field = null;
        }

        private void ExecuteBack()
        {
            CompletionStatusText = string.Empty;

            if (IsCompletionStep)
            {
                CurrentStep = VerificationStep;
            }
            else if (IsVerificationStep)
            {
                CurrentStep = RepositoryStep;
            }
            else if (IsRepositoryStep)
            {
                CurrentStep = DatabaseStep;
            }
        }

        private void ExecuteCancelSetupCompletion()
        {
            if (_setupCompletionCancellation is null
                || _setupCompletionCancellation.IsCancellationRequested)
            {
                return;
            }

            CompletionStatusText = Resources.Setup_CompletionCancellationRequested;
            _setupCompletionCancellation.Cancel();
            RaiseCommandStates();
        }

        private async void ExecuteCompleteSetup()
        {
            if (_applicationConfiguration is null)
            {
                CompletionStatusText = Resources.Setup_CompletionFailed;
                return;
            }

            CancelAndDispose(ref _setupCompletionCancellation);

            var operationCancellation = new CancellationTokenSource();
            _setupCompletionCancellation = operationCancellation;

            try
            {
                IsCompletingSetup = true;
                CompletionStatusText = Resources.Setup_CompletionRunning;

                var request = new SetupCompletionRequest(
                    _applicationConfiguration,
                    RepositoryLocationPath);

                var result = await _setupCompletionService
                    .CompleteAsync(request, operationCancellation.Token)
                    .ConfigureAwait(true);

                ApplySetupCompletionResult(result);
            }
            catch (OperationCanceledException)
            {
                CompletionStatusText = Resources.Setup_CompletionCanceled;
            }
            catch
            {
                // The setup-completion service normally converts technical failures
                // into structured results. This boundary protects the UI from an
                // unexpected implementation or navigation exception.
                CompletionStatusText = Resources.Setup_CompletionFailed;
            }
            finally
            {
                CompleteOperation(
                    ref _setupCompletionCancellation,
                    operationCancellation);

                IsCompletingSetup = false;
            }
        }

        private void ExecuteContinue()
        {
            CompletionStatusText = string.Empty;

            if (IsDatabaseStep && CanContinueFromDatabase)
            {
                CurrentStep = RepositoryStep;
            }
            else if (IsRepositoryStep && CanContinueFromRepository)
            {
                CurrentStep = VerificationStep;
            }
            else if (IsVerificationStep)
            {
                CurrentStep = CompletionStep;
            }
        }

        private void ExecuteSelectRepositoryLocation()
        {
            var fileDialogService = _wpf.DialogService?.FileDialogService
                ?? throw new InvalidOperationException("The WPF file dialog service has not been initialized.");

            var selectedPath = fileDialogService.SelectFolder(
                Resources.Setup_RepositoryFolderDialogTitle,
                string.IsNullOrWhiteSpace(RepositoryLocationPath)
                    ? null
                    : RepositoryLocationPath);

            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                RepositoryLocationPath = selectedPath;
            }
        }

        private async void ExecuteTestDatabaseConnection()
        {
            CancelAndDispose(ref _databaseTestCancellation);

            var operationCancellation = new CancellationTokenSource();
            _databaseTestCancellation = operationCancellation;

            try
            {
                IsTestingDatabase = true;
                IsDatabaseConnectionSuccessful = null;
                DatabaseStatusText = Resources.Setup_DatabaseConnectionTesting;

                var isSuccessful = await _configurationService
                    .TestDatabaseConnectionAsync(
                        new SetupDatabaseConfiguration(ConnectionString),
                        operationCancellation.Token)
                    .ConfigureAwait(true);

                IsDatabaseConnectionSuccessful = isSuccessful;
                DatabaseStatusText = isSuccessful
                    ? Resources.Setup_DatabaseConnectionSuccessful
                    : Resources.Setup_DatabaseConnectionFailed;
            }
            catch (OperationCanceledException)
            {
                IsDatabaseConnectionSuccessful = null;
                DatabaseStatusText = Resources.Setup_DatabaseConnectionCanceled;
            }
            catch
            {
                // The UI receives a localized status while technical diagnostics
                // remain inside the service boundary.
                IsDatabaseConnectionSuccessful = false;
                DatabaseStatusText = Resources.Setup_DatabaseConnectionFailed;
            }
            finally
            {
                CompleteOperation(
                    ref _databaseTestCancellation,
                    operationCancellation);

                IsTestingDatabase = false;
            }
        }

        private async void ExecuteValidateRepositoryLocation()
        {
            CancelAndDispose(ref _repositoryValidationCancellation);

            var operationCancellation = new CancellationTokenSource();
            _repositoryValidationCancellation = operationCancellation;

            try
            {
                IsValidatingRepositoryLocation = true;
                IsRepositoryLocationValid = null;
                RepositoryStatusText = Resources.Setup_RepositoryValidationRunning;

                var result = await _repositoryLocationValidationService
                    .ValidateAsync(
                        RepositoryLocationPath,
                        operationCancellation.Token)
                    .ConfigureAwait(true);

                ApplyRepositoryValidationResult(result);
            }
            catch (OperationCanceledException)
            {
                IsRepositoryLocationValid = null;
                RepositoryStatusText = Resources.Setup_RepositoryValidationCanceled;
            }
            catch
            {
                // Technical file-system details remain outside the user-facing
                // setup state.
                IsRepositoryLocationValid = false;
                RepositoryStatusText = Resources.Setup_RepositoryValidationFailed;
            }
            finally
            {
                CompleteOperation(
                    ref _repositoryValidationCancellation,
                    operationCancellation);

                IsValidatingRepositoryLocation = false;
            }
        }

        private void NavigateToImagingWorkbench()
        {
            _regionManager.RequestNavigate(
                RegionNames.ContentRegion,
                NavigationNames.Imaging);
        }

        private void RaiseCommandStates()
        {
            RaisePropertyChanged(nameof(CanContinueFromDatabase));
            RaisePropertyChanged(nameof(CanContinueFromRepository));
            RaisePropertyChanged(nameof(IsSetupInteractionEnabled));

            _backCommand?.RaiseCanExecuteChanged();
            _cancelSetupCompletionCommand?.RaiseCanExecuteChanged();
            _completeSetupCommand?.RaiseCanExecuteChanged();
            _continueCommand?.RaiseCanExecuteChanged();
            _selectRepositoryLocationCommand?.RaiseCanExecuteChanged();
            _testDatabaseConnectionCommand?.RaiseCanExecuteChanged();
            _validateRepositoryLocationCommand?.RaiseCanExecuteChanged();
        }

        private sealed class SetupDatabaseConfiguration : IDatabaseConfiguration
        {
            public SetupDatabaseConfiguration(string connectionString) =>
                ConnectionString = connectionString;

            /// <inheritdoc/>
            public string ConnectionString { get; }
        }
    }
}