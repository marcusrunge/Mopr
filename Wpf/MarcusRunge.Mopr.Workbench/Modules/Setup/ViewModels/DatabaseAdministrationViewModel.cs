using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels
{
    /// <summary>
    /// Provides controlled local editing of the machine-wide database configuration.
    /// </summary>
    public sealed class DatabaseAdministrationViewModel : BindableBase
    {
        private readonly CancellationToken _applicationStopping;
        private readonly IMachineConfigurationService _configurationService;
        private IApplicationConfiguration? _loadedConfiguration;
        private CancellationTokenSource? _operationCancellation;
        private string _connectionString = string.Empty;
        private string _originalConnectionString = string.Empty;
        private bool _isBusy;
        private bool _isConnectionTestSuccessful;
        private bool _isRestartRequired;
        private DelegateCommand? _resetCommand;
        private DelegateCommand? _saveCommand;
        private string _statusText = string.Empty;
        private DelegateCommand? _testConnectionCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseAdministrationViewModel"/> class.
        /// </summary>
        public DatabaseAdministrationViewModel(IMachineConfigurationService configurationService, ILifetimeService lifetimeService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            ArgumentNullException.ThrowIfNull(lifetimeService);
            _applicationStopping = lifetimeService.ApplicationStopping;
            _ = LoadAsync(_applicationStopping);
        }

        public string ConnectionString
        {
            get => _connectionString;
            set
            {
                if (!SetProperty(ref _connectionString, value)) return;
                IsConnectionTestSuccessful = false;
                IsRestartRequired = false;
                StatusText = string.Empty;
                RaiseCommandStates();
            }
        }
        public bool HasChanges => !string.Equals(ConnectionString, _originalConnectionString, StringComparison.Ordinal);
        public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) { RaisePropertyChanged(nameof(IsInteractionEnabled)); RaiseCommandStates(); } } }
        public bool IsConnectionTestSuccessful { get => _isConnectionTestSuccessful; private set { if (SetProperty(ref _isConnectionTestSuccessful, value)) RaiseCommandStates(); } }
        public bool IsInteractionEnabled => !IsBusy;
        public bool IsRestartRequired { get => _isRestartRequired; private set => SetProperty(ref _isRestartRequired, value); }
        public DelegateCommand ResetCommand => _resetCommand ??= new DelegateCommand(ExecuteReset, CanReset);
        public DelegateCommand SaveCommand => _saveCommand ??= new DelegateCommand(ExecuteSave, CanSave);
        public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
        public DelegateCommand TestConnectionCommand => _testConnectionCommand ??= new DelegateCommand(ExecuteTestConnection, CanTestConnection);

        private bool CanReset() => !IsBusy && HasChanges;
        private bool CanSave() => !IsBusy && HasChanges && IsConnectionTestSuccessful && _loadedConfiguration is not null && _configurationService.CanModify;
        private bool CanTestConnection() => !IsBusy && !string.IsNullOrWhiteSpace(ConnectionString) && _configurationService.CanModify;
        private static void CancelAndDispose(ref CancellationTokenSource? cancellation) { cancellation?.Cancel(); cancellation?.Dispose(); cancellation = null; }
        private CancellationTokenSource CreateOperationCancellation() { CancelAndDispose(ref _operationCancellation); _operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(_applicationStopping); return _operationCancellation; }

        private void ExecuteReset()
        {
            ConnectionString = _originalConnectionString;
            IsConnectionTestSuccessful = false;
            IsRestartRequired = false;
            StatusText = Resources.DatabaseAdministration_ChangesDiscarded;
            RaiseCommandStates();
        }

        private async void ExecuteSave()
        {
            var source = _loadedConfiguration;
            if (source is null) return;
            var cancellation = CreateOperationCancellation();
            try
            {
                IsBusy = true;
                StatusText = Resources.DatabaseAdministration_Saving;
                var configuration = new AdministrativeApplicationConfiguration(new AdministrativeDatabaseConfiguration(ConnectionString), source.Repository, source.Security, source.IsSetupComplete, source.SetupVersion);
                await _configurationService.SaveAsync(configuration, cancellation.Token).ConfigureAwait(true);
                _loadedConfiguration = configuration;
                _originalConnectionString = ConnectionString;
                IsConnectionTestSuccessful = false;
                IsRestartRequired = true;
                StatusText = Resources.DatabaseAdministration_Saved;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { StatusText = Resources.DatabaseAdministration_Canceled; }
            catch (UnauthorizedAccessException) { StatusText = Resources.DatabaseAdministration_AccessDenied; }
            catch { StatusText = Resources.DatabaseAdministration_SaveFailed; }
            finally { IsBusy = false; if (ReferenceEquals(_operationCancellation, cancellation)) { cancellation.Dispose(); _operationCancellation = null; } RaiseCommandStates(); }
        }

        private async void ExecuteTestConnection()
        {
            var cancellation = CreateOperationCancellation();
            try
            {
                IsBusy = true;
                IsConnectionTestSuccessful = false;
                StatusText = Resources.DatabaseAdministration_Testing;
                IsConnectionTestSuccessful = await _configurationService.TestDatabaseConnectionAsync(new AdministrativeDatabaseConfiguration(ConnectionString), cancellation.Token).ConfigureAwait(true);
                StatusText = IsConnectionTestSuccessful ? Resources.DatabaseAdministration_TestSucceeded : Resources.DatabaseAdministration_TestFailed;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { StatusText = Resources.DatabaseAdministration_Canceled; }
            catch { IsConnectionTestSuccessful = false; StatusText = Resources.DatabaseAdministration_TestFailed; }
            finally { IsBusy = false; if (ReferenceEquals(_operationCancellation, cancellation)) { cancellation.Dispose(); _operationCancellation = null; } RaiseCommandStates(); }
        }

        private async Task LoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                IsBusy = true;
                StatusText = Resources.DatabaseAdministration_Loading;
                _loadedConfiguration = await _configurationService.LoadAsync(cancellationToken).ConfigureAwait(true);
                _originalConnectionString = _loadedConfiguration.Database.ConnectionString;
                ConnectionString = _originalConnectionString;
                StatusText = string.Empty;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { StatusText = Resources.DatabaseAdministration_Canceled; }
            catch { StatusText = Resources.DatabaseAdministration_LoadFailed; }
            finally { IsBusy = false; RaiseCommandStates(); }
        }

        private void RaiseCommandStates()
        {
            RaisePropertyChanged(nameof(HasChanges));
            _resetCommand?.RaiseCanExecuteChanged();
            _saveCommand?.RaiseCanExecuteChanged();
            _testConnectionCommand?.RaiseCanExecuteChanged();
        }

        private sealed class AdministrativeApplicationConfiguration(IDatabaseConfiguration database, IRepositoryConfiguration repository, ISecurityConfiguration security, bool isSetupComplete, int setupVersion) : IApplicationConfiguration
        {
            public IDatabaseConfiguration Database { get; } = database;
            public bool IsSetupComplete { get; } = isSetupComplete;
            public IRepositoryConfiguration Repository { get; } = repository;
            public ISecurityConfiguration Security { get; } = security;
            public int SetupVersion { get; } = setupVersion;
        }
        private sealed class AdministrativeDatabaseConfiguration(string connectionString) : IDatabaseConfiguration { public string ConnectionString { get; } = connectionString; }
    }
}
