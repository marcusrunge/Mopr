using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using MarcusRunge.Mopr.Workbench.Core.Events;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System;
using System.Threading;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels
{
    /// <summary>
    /// Coordinates the machine-wide MOPR setup workflow.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="SetupViewModel"/> class.
    /// </remarks>
    public sealed class SetupViewModel(IMachineConfigurationService configurationService, IRepositoryLocationValidationService repositoryLocationValidationService, ISetupCompletionService setupCompletionService, IApplication application, IEventAggregator eventAggregator) : BindableBase, INavigationAware, IConfirmNavigationRequest
    {
        private const int CompletionStep = 4;
        private const int DatabaseStep = 1;
        private const string DefaultLocalDbConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=Mopr;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        private const int RepositoryStep = 2;
        private const int VerificationStep = 3;

        private readonly IApplication _application = application ?? throw new ArgumentNullException(nameof(application));
        private readonly IMachineConfigurationService _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        private readonly IEventAggregator _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        private readonly IRepositoryLocationValidationService _repositoryLocationValidationService = repositoryLocationValidationService ?? throw new ArgumentNullException(nameof(repositoryLocationValidationService));
        private readonly ISetupCompletionService _setupCompletionService = setupCompletionService ?? throw new ArgumentNullException(nameof(setupCompletionService));

        private IApplicationConfiguration? _applicationConfiguration;
        private DelegateCommand? _backCommand;
        private DelegateCommand? _cancelSetupCompletionCommand;
        private DelegateCommand? _completeSetupCommand;
        private string _completionStatusText = string.Empty;
        private string _connectionString = string.Empty;
        private DelegateCommand? _continueCommand;
        private int _currentStep = DatabaseStep;
        private string _databaseStatusText = string.Empty;
        private CancellationTokenSource? _databaseTestCancellation;
        private bool _isCompletingSetup;
        private bool? _isDatabaseConnectionSuccessful;
        private bool _isLoading;
        private bool? _isRepositoryLocationValid;
        private bool _isSetupComplete;
        private bool _isTestingDatabase;
        private bool _isValidatingRepositoryLocation;
        private string _repositoryLocationPath = string.Empty;
        private string _repositoryStatusText = string.Empty;
        private CancellationTokenSource? _repositoryValidationCancellation;
        private DelegateCommand? _selectRepositoryLocationCommand;
        private CancellationTokenSource? _setupCompletionCancellation;
        private DelegateCommand? _testDatabaseConnectionCommand;
        private DelegateCommand? _useLocalDatabaseCommand;
        private DelegateCommand? _validateRepositoryLocationCommand;
        public DelegateCommand BackCommand => _backCommand ??= new DelegateCommand(ExecuteBack, CanExecuteBack);

        public DelegateCommand CancelSetupCompletionCommand => _cancelSetupCompletionCommand ??= new DelegateCommand(ExecuteCancelSetupCompletion, CanExecuteCancelSetupCompletion);

        public bool CanContinueFromDatabase => IsDatabaseConnectionSuccessful == true && !IsLoading && !IsTestingDatabase && !IsCompletingSetup;

        public bool CanContinueFromRepository => IsRepositoryLocationValid == true && !IsLoading && !IsValidatingRepositoryLocation && !IsCompletingSetup;

        public bool CanModify => _configurationService.CanModify;

        public DelegateCommand CompleteSetupCommand => _completeSetupCommand ??= new DelegateCommand(ExecuteCompleteSetup, CanExecuteCompleteSetup);

        public string CompletionStatusText
        {
            get => _completionStatusText;
            private set => SetProperty(ref _completionStatusText, value);
        }

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

        public DelegateCommand ContinueCommand => _continueCommand ??= new DelegateCommand(ExecuteContinue, CanExecuteContinue);

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

        public string DatabaseStatusText
        {
            get => _databaseStatusText;
            private set => SetProperty(ref _databaseStatusText, value);
        }

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

        public bool IsCompletionStep => CurrentStep == CompletionStep;

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

        public bool IsDatabaseStep => CurrentStep == DatabaseStep;

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (!SetProperty(ref _isLoading, value))
                {
                    return;
                }

                RaisePropertyChanged(nameof(IsSetupInteractionEnabled));
                RaiseCommandStates();
            }
        }

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

        public bool IsRepositoryStep => CurrentStep == RepositoryStep;

        public bool IsSetupComplete
        {
            get => _isSetupComplete;
            private set => SetProperty(ref _isSetupComplete, value);
        }

        public bool IsSetupInteractionEnabled => !IsLoading && !IsCompletingSetup;

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

        public bool IsVerificationStep => CurrentStep == VerificationStep;

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

        public string RepositoryStatusText
        {
            get => _repositoryStatusText;
            private set => SetProperty(ref _repositoryStatusText, value);
        }

        public DelegateCommand SelectRepositoryLocationCommand => _selectRepositoryLocationCommand ??= new DelegateCommand(ExecuteSelectRepositoryLocation, CanSelectRepositoryLocation);

        public DelegateCommand TestDatabaseConnectionCommand => _testDatabaseConnectionCommand ??= new DelegateCommand(ExecuteTestDatabaseConnection, CanTestDatabaseConnection);

        public DelegateCommand UseLocalDatabaseCommand => _useLocalDatabaseCommand ??= new DelegateCommand(ExecuteUseLocalDatabase, CanUseLocalDatabase);

        public DelegateCommand ValidateRepositoryLocationCommand => _validateRepositoryLocationCommand ??= new DelegateCommand(ExecuteValidateRepositoryLocation, CanValidateRepositoryLocation);

        /// <inheritdoc/>
        public void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
        {
            ArgumentNullException.ThrowIfNull(continuationCallback);

            // Navigation remains blocked until the protected transition and
            // any required compensation have completed.
            continuationCallback(!IsCompletingSetup);
        }

        /// <inheritdoc/>
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <inheritdoc/>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            CancelAndDispose(ref _databaseTestCancellation);
            CancelAndDispose(ref _repositoryValidationCancellation);
            CancelAndDispose(ref _setupCompletionCancellation);
        }

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

        private static void CompleteOperation(ref CancellationTokenSource? field, CancellationTokenSource operationCancellation)
        {
            if (!ReferenceEquals(field, operationCancellation))
            {
                return;
            }

            operationCancellation.Dispose();
            field = null;
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
            CompletionStatusText = result.Status switch
            {
                SetupCompletionStatus.Completed => Resources.Setup_CompletionSuccessful,
                SetupCompletionStatus.DatabaseValidationFailed => Resources.Setup_CompletionDatabaseValidationFailed,
                SetupCompletionStatus.RepositoryValidationFailed => Resources.Setup_CompletionRepositoryValidationFailed,
                SetupCompletionStatus.Canceled => Resources.Setup_CompletionCanceled,
                SetupCompletionStatus.FailedAndRollbackFailed => Resources.Setup_CompletionRollbackFailed,
                _ => Resources.Setup_CompletionFailed
            };

            if (result.IsSuccessful)
            {
                IsSetupComplete = true;
            }
        }

        private bool CanExecuteBack() => CurrentStep > DatabaseStep && !IsLoading && !IsTestingDatabase && !IsValidatingRepositoryLocation && !IsCompletingSetup;

        private bool CanExecuteCancelSetupCompletion() => IsCompletingSetup && _setupCompletionCancellation is not null && !_setupCompletionCancellation.IsCancellationRequested;

        private bool CanExecuteCompleteSetup() => CanModify && IsCompletionStep && !IsLoading && !IsTestingDatabase && !IsValidatingRepositoryLocation && !IsCompletingSetup && IsDatabaseConnectionSuccessful == true && IsRepositoryLocationValid == true && _applicationConfiguration is not null && !string.IsNullOrWhiteSpace(ConnectionString) && !string.IsNullOrWhiteSpace(RepositoryLocationPath);

        private bool CanExecuteContinue() => !IsCompletingSetup && (IsDatabaseStep ? CanContinueFromDatabase : IsRepositoryStep ? CanContinueFromRepository : IsVerificationStep);

        private bool CanSelectRepositoryLocation() => CanModify && IsRepositoryStep && !IsLoading && !IsValidatingRepositoryLocation && !IsCompletingSetup;

        private bool CanTestDatabaseConnection() => CanModify && IsDatabaseStep && !IsLoading && !IsTestingDatabase && !IsCompletingSetup && !string.IsNullOrWhiteSpace(ConnectionString);

        private bool CanUseLocalDatabase() => CanModify && IsDatabaseStep && !IsLoading && !IsTestingDatabase && !IsCompletingSetup;

        private bool CanValidateRepositoryLocation() => CanModify && IsRepositoryStep && !IsLoading && !IsValidatingRepositoryLocation && !IsCompletingSetup && !string.IsNullOrWhiteSpace(RepositoryLocationPath);

        private SetupApplicationConfiguration CreateCompletionConfiguration()
        {
            var source = _applicationConfiguration ?? throw new InvalidOperationException("The machine-wide setup configuration has not been loaded.");

            // The loaded configuration may still contain the database value that
            // existed before the currently entered connection string was validated.
            return new SetupApplicationConfiguration(new SetupDatabaseConfiguration(ConnectionString), source.Repository, source.Security, source.SetupVersion);
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
            if (_setupCompletionCancellation is null || _setupCompletionCancellation.IsCancellationRequested)
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
            var setupCompleted = false;

            try
            {
                IsCompletingSetup = true;
                CompletionStatusText = Resources.Setup_CompletionRunning;

                var request = new SetupCompletionRequest(CreateCompletionConfiguration(), RepositoryLocationPath);
                var result = await _setupCompletionService.CompleteAsync(request, operationCancellation.Token).ConfigureAwait(true);

                ApplySetupCompletionResult(result);
                setupCompleted = result.IsSuccessful;
            }
            catch (OperationCanceledException)
            {
                CompletionStatusText = Resources.Setup_CompletionCanceled;
            }
            catch
            {
                // The service normally converts technical failures into structured
                // results. This boundary protects the UI from unexpected failures.
                CompletionStatusText = Resources.Setup_CompletionFailed;
            }
            finally
            {
                CompleteOperation(ref _setupCompletionCancellation, operationCancellation);
                IsCompletingSetup = false;
            }

            if (setupCompleted)
            {
                // The composition root owns configuration publication, Persistence
                // initialization, first-user detection, MIRAS and final navigation.
                _eventAggregator.GetEvent<MachineSetupCompletedEvent>().Publish();
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
            var fileDialogService = _application.DialogService?.FileDialogService ?? throw new InvalidOperationException("The WPF file dialog service has not been initialized.");
            var selectedPath = fileDialogService.SelectFolder(Resources.Setup_RepositoryFolderDialogTitle, string.IsNullOrWhiteSpace(RepositoryLocationPath) ? null : RepositoryLocationPath);

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

                var isSuccessful = await _configurationService.TestDatabaseConnectionAsync(new SetupDatabaseConfiguration(ConnectionString), operationCancellation.Token).ConfigureAwait(true);

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
                IsDatabaseConnectionSuccessful = false;
                DatabaseStatusText = Resources.Setup_DatabaseConnectionFailed;
            }
            finally
            {
                CompleteOperation(ref _databaseTestCancellation, operationCancellation);
                IsTestingDatabase = false;
            }
        }

        private void ExecuteUseLocalDatabase() => ConnectionString = DefaultLocalDbConnectionString;

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

                var result = await _repositoryLocationValidationService.ValidateAsync(RepositoryLocationPath, operationCancellation.Token).ConfigureAwait(true);
                ApplyRepositoryValidationResult(result);
            }
            catch (OperationCanceledException)
            {
                IsRepositoryLocationValid = null;
                RepositoryStatusText = Resources.Setup_RepositoryValidationCanceled;
            }
            catch
            {
                IsRepositoryLocationValid = false;
                RepositoryStatusText = Resources.Setup_RepositoryValidationFailed;
            }
            finally
            {
                CompleteOperation(ref _repositoryValidationCancellation, operationCancellation);
                IsValidatingRepositoryLocation = false;
            }
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
            _useLocalDatabaseCommand?.RaiseCanExecuteChanged();
            _validateRepositoryLocationCommand?.RaiseCanExecuteChanged();
        }

        private sealed class SetupApplicationConfiguration(IDatabaseConfiguration database, IRepositoryConfiguration repository, ISecurityConfiguration security, int setupVersion) : IApplicationConfiguration
        {
            public IDatabaseConfiguration Database { get; } = database;

            public bool IsSetupComplete => false;

            public IRepositoryConfiguration Repository { get; } = repository;

            public ISecurityConfiguration Security { get; } = security;

            public int SetupVersion { get; } = setupVersion;
        }

        private sealed class SetupDatabaseConfiguration(string connectionString) : IDatabaseConfiguration
        {
            public string ConnectionString { get; } = connectionString;
        }
    }
}