using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Modules.Setup.Properties;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels
{
    /// <summary>
    /// Provides the initial state and database validation of the machine-wide MOPR setup.
    /// </summary>
    public sealed class SetupViewModel : BindableBase, INavigationAware
    {
        private readonly IMachineConfigurationService _configurationService;
        private string _connectionString = string.Empty;
        private string _databaseStatusText = string.Empty;
        private bool? _isDatabaseConnectionSuccessful;
        private bool _isLoading;
        private bool _isSetupComplete;
        private bool _isTestingDatabase;

        public SetupViewModel(IMachineConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            TestDatabaseConnectionCommand = new DelegateCommand(ExecuteTestDatabaseConnection, CanTestDatabaseConnection);
        }

        /// <summary>
        /// Gets a value indicating whether the current process may change the machine-wide configuration.
        /// </summary>
        public bool CanModify => _configurationService.CanModify;

        /// <summary>
        /// Gets or sets the SQL Server connection string entered during setup.
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
                TestDatabaseConnectionCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets the current user-oriented database status text.
        /// </summary>
        public string DatabaseStatusText
        {
            get => _databaseStatusText;
            private set => SetProperty(ref _databaseStatusText, value);
        }

        /// <summary>
        /// Gets a value indicating whether the most recent database connection test succeeded.
        /// </summary>
        public bool? IsDatabaseConnectionSuccessful
        {
            get => _isDatabaseConnectionSuccessful;
            private set => SetProperty(ref _isDatabaseConnectionSuccessful, value);
        }

        /// <summary>
        /// Gets a value indicating whether the machine configuration is loading.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    TestDatabaseConnectionCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the stored setup is complete.
        /// </summary>
        public bool IsSetupComplete
        {
            get => _isSetupComplete;
            private set => SetProperty(ref _isSetupComplete, value);
        }

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
                    TestDatabaseConnectionCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets the command that tests the entered database connection.
        /// </summary>
        public DelegateCommand TestDatabaseConnectionCommand { get; }

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
        }

        private bool CanTestDatabaseConnection() => CanModify && !IsLoading && !IsTestingDatabase && !string.IsNullOrWhiteSpace(ConnectionString);

        private async void ExecuteTestDatabaseConnection()
        {
            try
            {
                IsTestingDatabase = true;
                IsDatabaseConnectionSuccessful = null;
                DatabaseStatusText = Resources.Setup_DatabaseConnectionTesting;

                var isSuccessful = await _configurationService.TestDatabaseConnectionAsync(new SetupDatabaseConfiguration(ConnectionString)).ConfigureAwait(true);

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
            }
        }

        private sealed class SetupDatabaseConfiguration(string connectionString) : IDatabaseConfiguration
        {
            /// <inheritdoc/>
            public string ConnectionString { get; } = connectionString;
        }
    }
}