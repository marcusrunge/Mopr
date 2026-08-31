using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Stores the machine-wide MOPR configuration below the Windows program-data directory.
    /// </summary>
    internal sealed class ApplicationConfigurationStore(IAdministrativeAuthorizationService authorizationService, IMachineConfigurationPathProvider pathProvider, IMachineConfigurationProtectionService protectionService) : IApplicationConfigurationStore
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly IAdministrativeAuthorizationService _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        private readonly IMachineConfigurationPathProvider _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        private readonly IMachineConfigurationProtectionService _protectionService = protectionService ?? throw new ArgumentNullException(nameof(protectionService));

        /// <inheritdoc/>
        public string ConfigurationFilePath => _pathProvider.ConfigurationFilePath;

        /// <inheritdoc/>
        public async Task<IApplicationConfiguration> LoadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(ConfigurationFilePath))
            {
                return new ApplicationConfiguration();
            }

            await using var stream = new FileStream(ConfigurationFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

            var configuration = await JsonSerializer.DeserializeAsync<ApplicationConfiguration>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);

            return ValidateLoadedConfiguration(configuration);
        }

        /// <inheritdoc/>
        public async Task SaveAsync(IApplicationConfiguration configuration, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            _authorizationService.DemandElevatedAdministrator();
            cancellationToken.ThrowIfCancellationRequested();

            Directory.CreateDirectory(_pathProvider.ConfigurationDirectoryPath);
            _protectionService.ProtectDirectory(_pathProvider.ConfigurationDirectoryPath);

            var serializableConfiguration = CreateSerializableConfiguration(configuration);
            var temporaryFilePath = Path.Combine(_pathProvider.ConfigurationDirectoryPath, $".{Path.GetFileName(ConfigurationFilePath)}.{Guid.NewGuid():N}.tmp");

            try
            {
                await WriteTemporaryFileAsync(temporaryFilePath, serializableConfiguration, cancellationToken).ConfigureAwait(false);

                File.Move(temporaryFilePath, ConfigurationFilePath, overwrite: true);

                _protectionService.ProtectFile(ConfigurationFilePath);
            }
            finally
            {
                TryDeleteTemporaryFile(temporaryFilePath);
            }
        }

        private static ApplicationConfiguration CreateSerializableConfiguration(IApplicationConfiguration configuration) =>
            new()
            {
                DatabaseConfiguration = new DatabaseConfiguration
                {
                    ConnectionString = configuration.Database.ConnectionString
                },
                IsSetupComplete = configuration.IsSetupComplete,
                RepositoryConfiguration = new RepositoryConfiguration
                {
                    AutomaticallyRepairPaths = configuration.Repository.AutomaticallyRepairPaths
                },
                SecurityConfiguration = new SecurityConfiguration
                {
                    AllowSelfDeletion = configuration.Security.AllowSelfDeletion,
                    AllowSelfModification = configuration.Security.AllowSelfModification,
                    HideOtherUsersFromRegularUsers = configuration.Security.HideOtherUsersFromRegularUsers
                },
                SetupVersion = configuration.SetupVersion
            };

        private static ApplicationConfiguration ValidateLoadedConfiguration(ApplicationConfiguration? configuration)
        {
            if (configuration is null)
            {
                throw new InvalidDataException("The machine-wide MOPR configuration does not contain a valid JSON object.");
            }

            if (configuration.SetupVersion <= 0)
            {
                throw new InvalidDataException("The machine-wide MOPR configuration contains an invalid setup version.");
            }

            if (configuration.IsSetupComplete && string.IsNullOrWhiteSpace(configuration.Database.ConnectionString))
            {
                throw new InvalidDataException("The completed machine-wide MOPR configuration does not contain a database connection string.");
            }

            return configuration;
        }

        private static void TryDeleteTemporaryFile(string temporaryFilePath)
        {
            try
            {
                if (File.Exists(temporaryFilePath))
                {
                    File.Delete(temporaryFilePath);
                }
            }
            catch
            {
                // Cleanup must not hide the original serialization, replacement,
                // authorization or access-control exception.
            }
        }

        private static async Task WriteTemporaryFileAsync(string temporaryFilePath, ApplicationConfiguration configuration, CancellationToken cancellationToken)
        {
            await using var stream = new FileStream(temporaryFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true);

            await JsonSerializer.SerializeAsync(stream, configuration, SerializerOptions, cancellationToken).ConfigureAwait(false);

            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}