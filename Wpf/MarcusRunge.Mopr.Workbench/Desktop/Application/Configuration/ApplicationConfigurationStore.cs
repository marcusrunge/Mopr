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
    /// Stores the protected machine-wide MOPR configuration below the Windows program-data directory.
    /// </summary>
    internal sealed class ApplicationConfigurationStore(IAdministrativeAuthorizationService authorizationService, IMachineConfigurationPathProvider pathProvider, IMachineConfigurationProtectionService protectionService) : IApplicationConfigurationStore
    {
        private const int CurrentEnvelopeFormatVersion = 1;
        private const string CurrentProtectionMethod = "WindowsDpapiLocalMachine";

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

            var envelope = await JsonSerializer
                .DeserializeAsync<MachineConfigurationEnvelope>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);

            ValidateEnvelope(envelope);

            cancellationToken.ThrowIfCancellationRequested();

            byte[] protectedConfiguration;

            try
            {
                protectedConfiguration = Convert.FromBase64String(envelope!.Payload);
            }
            catch (FormatException exception)
            {
                throw new InvalidDataException("The protected machine-wide MOPR configuration payload is not valid Base64 data.", exception);
            }

            byte[]? unprotectedConfiguration = null;

            try
            {
                unprotectedConfiguration = _protectionService.UnprotectData(protectedConfiguration);

                cancellationToken.ThrowIfCancellationRequested();

                var configuration = JsonSerializer.Deserialize<ApplicationConfiguration>(unprotectedConfiguration, SerializerOptions);

                return ValidateLoadedConfiguration(configuration);
            }
            finally
            {
                // Unprotected configuration bytes may contain database credentials
                // and must not remain in managed buffers longer than necessary.
                if (unprotectedConfiguration is not null)
                {
                    Array.Clear(
                        unprotectedConfiguration,
                        0,
                        unprotectedConfiguration.Length);
                }

                Array.Clear(
                    protectedConfiguration,
                    0,
                    protectedConfiguration.Length);
            }
        }

        /// <inheritdoc/>
        public async Task SaveAsync(IApplicationConfiguration configuration, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            _authorizationService.DemandElevatedAdministrator();
            cancellationToken.ThrowIfCancellationRequested();

            Directory.CreateDirectory(_pathProvider.ConfigurationDirectoryPath);
            _protectionService.ProtectDirectory(_pathProvider.ConfigurationDirectoryPath);

            var serializableConfiguration =
                CreateSerializableConfiguration(configuration);

            byte[]? unprotectedConfiguration = null;
            byte[]? protectedConfiguration = null;

            var temporaryFilePath = Path.Combine(_pathProvider.ConfigurationDirectoryPath, $".{Path.GetFileName(ConfigurationFilePath)}.{Guid.NewGuid():N}.tmp");

            try
            {
                unprotectedConfiguration = JsonSerializer.SerializeToUtf8Bytes(serializableConfiguration, SerializerOptions);

                cancellationToken.ThrowIfCancellationRequested();

                protectedConfiguration = _protectionService.ProtectData(unprotectedConfiguration);

                cancellationToken.ThrowIfCancellationRequested();

                var envelope = new MachineConfigurationEnvelope
                {
                    FormatVersion = CurrentEnvelopeFormatVersion,
                    Payload = Convert.ToBase64String(protectedConfiguration),
                    ProtectionMethod = CurrentProtectionMethod
                };

                await WriteTemporaryFileAsync(temporaryFilePath, envelope, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                File.Move(
                    temporaryFilePath, ConfigurationFilePath, overwrite: true);

                _protectionService.ProtectFile(ConfigurationFilePath);
            }
            finally
            {
                // Both arrays are cleared deliberately. The first contains the
                // complete plaintext configuration, while the second may contain
                // implementation-specific protected material.
                if (unprotectedConfiguration is not null)
                {
                    Array.Clear(unprotectedConfiguration, 0, unprotectedConfiguration.Length);
                }

                if (protectedConfiguration is not null)
                {
                    Array.Clear(protectedConfiguration, 0, protectedConfiguration.Length);
                }

                TryDeleteTemporaryFile(temporaryFilePath);
            }
        }

        private static ApplicationConfiguration CreateSerializableConfiguration(
            IApplicationConfiguration configuration) => new()
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
                throw new InvalidDataException("The protected machine-wide MOPR configuration does not contain a valid JSON object.");
            }

            if (configuration.SetupVersion <= 0)
            {
                throw new InvalidDataException("The protected machine-wide MOPR configuration contains an invalid setup version.");
            }

            if (configuration.IsSetupComplete && string.IsNullOrWhiteSpace(configuration.Database.ConnectionString))
            {
                throw new InvalidDataException("The completed machine-wide MOPR configuration does not contain a database connection string.");
            }

            return configuration;
        }

        private static void ValidateEnvelope(MachineConfigurationEnvelope? envelope)
        {
            if (envelope is null)
            {
                throw new InvalidDataException("The machine-wide MOPR configuration does not contain a valid protection envelope.");
            }

            if (envelope.FormatVersion != CurrentEnvelopeFormatVersion)
            {
                throw new InvalidDataException($"The machine-wide MOPR configuration uses unsupported envelope format version '{envelope.FormatVersion}'.");
            }

            if (!string.Equals(envelope.ProtectionMethod, CurrentProtectionMethod, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"The machine-wide MOPR configuration uses unsupported protection method '{envelope.ProtectionMethod}'.");
            }

            if (string.IsNullOrWhiteSpace(envelope.Payload))
            {
                throw new InvalidDataException(
                    "The protected machine-wide MOPR configuration payload is missing.");
            }
        }

        private static void TryDeleteTemporaryFile(
            string temporaryFilePath)
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
                // Cleanup must not hide the original serialization, protection,
                // replacement, authorization or access-control exception.
            }
        }

        private static async Task WriteTemporaryFileAsync(string temporaryFilePath, MachineConfigurationEnvelope envelope, CancellationToken cancellationToken)
        {
            await using var stream = new FileStream(temporaryFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true);

            await JsonSerializer.SerializeAsync(stream, envelope, SerializerOptions, cancellationToken).ConfigureAwait(false);

            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private sealed class MachineConfigurationEnvelope
        {
            public int FormatVersion { get; set; }

            public string Payload { get; set; } = string.Empty;

            public string ProtectionMethod { get; set; } = string.Empty;
        }
    }
}