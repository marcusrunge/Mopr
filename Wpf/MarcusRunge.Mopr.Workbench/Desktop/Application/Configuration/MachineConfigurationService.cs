using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Coordinates authorization, validation and storage of machine-wide MOPR configuration.
    /// </summary>
    internal sealed class MachineConfigurationService(IAdministrativeAuthorizationService authorizationService, IApplicationConfigurationStore configurationStore, IPersistence persistence) : IMachineConfigurationService
    {
        private readonly IAdministrativeAuthorizationService _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        private readonly IApplicationConfigurationStore _configurationStore = configurationStore ?? throw new ArgumentNullException(nameof(configurationStore));
        private readonly IPersistence _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));

        /// <inheritdoc/>
        public bool CanModify => _authorizationService.IsElevatedAdministrator;

        /// <inheritdoc/>
        public Task<IApplicationConfiguration> LoadAsync(CancellationToken cancellationToken = default) => _configurationStore.LoadAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task SaveAsync(IApplicationConfiguration configuration, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var validationResult = ValidateForSetupCompletion(configuration);

            if (!validationResult.IsValid)
            {
                throw new MachineConfigurationValidationException(validationResult);
            }

            // Authorization is checked before any machine-wide write operation.
            // The store repeats this check as a defense-in-depth boundary.
            _authorizationService.DemandElevatedAdministrator();

            await _configurationStore.SaveAsync(configuration, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<bool> TestDatabaseConnectionAsync(IDatabaseConfiguration configuration, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            cancellationToken.ThrowIfCancellationRequested();

            var result = await _persistence.TestConnectionAsync(new PersistenceConfiguration
            {
                ConnectionString = configuration.ConnectionString,
                Mode = PersistenceMode.SqlServer
            },
                    cancellationToken).ConfigureAwait(false);

            // Technical diagnostics remain inside the Persistence boundary. Setup
            // and Settings convert only the stable success state into localized UI.
            return result.IsSuccessful;
        }

        /// <inheritdoc/>
        public MachineConfigurationValidationResult ValidateForSetupCompletion(IApplicationConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var issues = new List<MachineConfigurationIssue>();

            if (configuration.SetupVersion <= 0)
            {
                issues.Add(MachineConfigurationIssue.InvalidSetupVersion);
            }

            if (string.IsNullOrWhiteSpace(
                configuration.Database.ConnectionString))
            {
                issues.Add(MachineConfigurationIssue.DatabaseConnectionStringMissing);
            }

            if (!configuration.IsSetupComplete)
            {
                issues.Add(MachineConfigurationIssue.SetupNotCompleted);
            }

            return issues.Count == 0 ? MachineConfigurationValidationResult.Success : new MachineConfigurationValidationResult(issues);
        }
    }
}