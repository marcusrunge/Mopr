using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Setup.ViewModels
{
    /// <summary>
    /// Provides the initial state of the machine-wide MOPR setup.
    /// </summary>
    public sealed class SetupViewModel : BindableBase, INavigationAware
    {
        private readonly IMachineConfigurationService _configurationService;
        private string _connectionString = string.Empty;
        private bool _isSetupComplete;
        private bool _isLoading;

        public SetupViewModel(IMachineConfigurationService configurationService) =>
            _configurationService = configurationService
                ?? throw new ArgumentNullException(
                    nameof(configurationService));

        /// <summary>
        /// Gets a value indicating whether the current process may change the
        /// machine-wide configuration.
        /// </summary>
        public bool CanModify => _configurationService.CanModify;

        /// <summary>
        /// Gets or sets the SQL Server connection string entered during setup.
        /// </summary>
        public string ConnectionString
        {
            get => _connectionString;
            set => SetProperty(ref _connectionString, value);
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
        /// Gets a value indicating whether the machine configuration is loading.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        /// <inheritdoc/>
        public bool IsNavigationTarget(
            NavigationContext navigationContext) => true;

        /// <inheritdoc/>
        public async void OnNavigatedTo(
            NavigationContext navigationContext)
        {
            try
            {
                IsLoading = true;

                var configuration = await _configurationService
                    .LoadAsync()
                    .ConfigureAwait(true);

                ConnectionString =
                    configuration.Database.ConnectionString;
                IsSetupComplete =
                    configuration.IsSetupComplete;

                RaisePropertyChanged(nameof(CanModify));
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <inheritdoc/>
        public void OnNavigatedFrom(
            NavigationContext navigationContext)
        {
        }
    }
}