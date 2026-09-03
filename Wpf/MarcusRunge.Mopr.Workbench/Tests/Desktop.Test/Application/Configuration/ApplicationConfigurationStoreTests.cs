using MarcusRunge.Mopr.Workbench.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Configuration
{
    public sealed class ApplicationConfigurationStoreTests
    {
        private const string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=MoprTests;User Id=mopr-user;Password=TopSecretPassword;";

        [Fact]
        public async Task LoadAsync_WhenConfigurationDoesNotExist_ReturnsIncompleteDefaults()
        {
            using var context = new StoreTestContext();

            var configuration = await context.Store.LoadAsync(TestContext.Current.CancellationToken);

            Assert.False(configuration.IsSetupComplete);
            Assert.Empty(configuration.Database.ConnectionString);
            Assert.Equal(ApplicationConfiguration.CurrentSetupVersion, configuration.SetupVersion);

            Assert.Equal(0, context.ProtectionService.UnprotectDataCallCount);
        }

        [Fact]
        public async Task SaveAndLoadAsync_WithElevatedAdministrator_PreservesProtectedConfiguration()
        {
            using var context = new StoreTestContext(isElevatedAdministrator: true);

            var expected = CreateCompletedConfiguration();

            await context.Store.SaveAsync(expected, TestContext.Current.CancellationToken);

            var actual = await context.Store.LoadAsync(TestContext.Current.CancellationToken);

            Assert.True(actual.IsSetupComplete);
            Assert.Equal(expected.SetupVersion, actual.SetupVersion);

            Assert.Equal(expected.Database.ConnectionString, actual.Database.ConnectionString);

            Assert.False(actual.Repository.AutomaticallyRepairPaths);

            Assert.True(actual.Security.AllowSelfDeletion);

            Assert.False(actual.Security.AllowSelfModification);

            Assert.False(actual.Security.HideOtherUsersFromRegularUsers);

            Assert.Equal(1, context.ProtectionService.ProtectDataCallCount);

            Assert.Equal(1, context.ProtectionService.UnprotectDataCallCount);

            Assert.Equal(1, context.ProtectionService.ProtectDirectoryCallCount);

            Assert.Equal(1, context.ProtectionService.ProtectFileCallCount);
        }

        [Fact]
        public async Task SaveAsync_WithSensitiveConnectionString_DoesNotWritePlaintextConfiguration()
        {
            using var context = new StoreTestContext(isElevatedAdministrator: true);

            await context.Store.SaveAsync(CreateCompletedConfiguration(), TestContext.Current.CancellationToken);

            var storedContent = await File.ReadAllTextAsync(context.ConfigurationFilePath, TestContext.Current.CancellationToken);

            Assert.DoesNotContain(ConnectionString, storedContent, StringComparison.Ordinal);

            Assert.DoesNotContain("TopSecretPassword", storedContent, StringComparison.Ordinal);

            Assert.DoesNotContain("mopr-user", storedContent, StringComparison.Ordinal);

            Assert.DoesNotContain(
                "MoprTests",
                storedContent,
                StringComparison.Ordinal);

            Assert.DoesNotContain("DatabaseConfiguration", storedContent, StringComparison.Ordinal);

            Assert.DoesNotContain("ConnectionString", storedContent, StringComparison.Ordinal);

            Assert.Contains("\"FormatVersion\": 1", storedContent, StringComparison.Ordinal);

            Assert.Contains("\"ProtectionMethod\": \"WindowsDpapiLocalMachine\"", storedContent, StringComparison.Ordinal);

            Assert.Contains("\"Payload\":", storedContent, StringComparison.Ordinal);

            Assert.Equal(1, context.ProtectionService.ProtectDataCallCount);
        }

        [Fact]
        public async Task SaveAsync_WithoutElevatedAdministrator_ThrowsAndCreatesNoFile()
        {
            using var context = new StoreTestContext(isElevatedAdministrator: false);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await context.Store.SaveAsync(new ApplicationConfiguration(), TestContext.Current.CancellationToken));

            Assert.False(File.Exists(context.ConfigurationFilePath));

            Assert.Equal(0, context.ProtectionService.ProtectDataCallCount);

            Assert.Equal(0, context.ProtectionService.ProtectDirectoryCallCount);

            Assert.Equal(0, context.ProtectionService.ProtectFileCallCount);
        }

        [Fact]
        public async Task LoadAsync_WithInvalidEnvelopeJson_ThrowsJsonException()
        {
            using var context = new StoreTestContext();

            await context.WriteRawConfigurationFileAsync("{ invalid json", TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<JsonException>(async () => await context.Store.LoadAsync(TestContext.Current.CancellationToken));

            Assert.Equal(0, context.ProtectionService.UnprotectDataCallCount);
        }

        [Fact]
        public async Task LoadAsync_WithPlaintextConfiguration_ThrowsInvalidDataException()
        {
            using var context = new StoreTestContext();

            await context.WriteRawConfigurationFileAsync(
                """
                {
                  "IsSetupComplete": true,
                  "SetupVersion": 1,
                  "DatabaseConfiguration": {
                    "ConnectionString": "Server=localhost;Database=Mopr;"
                  }
                }
                """, TestContext.Current.CancellationToken);

            var exception = await Assert.ThrowsAsync<InvalidDataException>(async () => await context.Store.LoadAsync(TestContext.Current.CancellationToken));

            Assert.Contains("format version", exception.Message, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(0, context.ProtectionService.UnprotectDataCallCount);
        }

        [Fact]
        public async Task LoadAsync_WithUnsupportedEnvelopeFormatVersion_ThrowsInvalidDataException()
        {
            using var context = new StoreTestContext();

            await context.WriteEnvelopeAsync(formatVersion: 2, protectionMethod: "WindowsDpapiLocalMachine", payload: Convert.ToBase64String([1, 2, 3]), TestContext.Current.CancellationToken);

            var exception = await Assert.ThrowsAsync<InvalidDataException>(async () => await context.Store.LoadAsync(TestContext.Current.CancellationToken));

            Assert.Contains("unsupported envelope format version", exception.Message, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(0, context.ProtectionService.UnprotectDataCallCount);
        }

        [Fact]
        public async Task LoadAsync_WithUnsupportedProtectionMethod_ThrowsInvalidDataException()
        {
            using var context = new StoreTestContext();

            await context.WriteEnvelopeAsync(formatVersion: 1, protectionMethod: "UnsupportedProtection", payload: Convert.ToBase64String([1, 2, 3]), TestContext.Current.CancellationToken);

            var exception = await Assert.ThrowsAsync<InvalidDataException>(async () => await context.Store.LoadAsync(TestContext.Current.CancellationToken));

            Assert.Contains("unsupported protection method", exception.Message, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(0, context.ProtectionService.UnprotectDataCallCount);
        }

        [Fact]
        public async Task LoadAsync_WithMissingPayload_ThrowsInvalidDataException()
        {
            using var context = new StoreTestContext();

            await context.WriteEnvelopeAsync(formatVersion: 1, protectionMethod: "WindowsDpapiLocalMachine", payload: string.Empty, TestContext.Current.CancellationToken);

            var exception = await Assert.ThrowsAsync<InvalidDataException>(async () => await context.Store.LoadAsync(TestContext.Current.CancellationToken));

            Assert.Contains(
                "payload is missing", exception.Message, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(0, context.ProtectionService.UnprotectDataCallCount);
        }

        [Fact]
        public async Task LoadAsync_WithInvalidBase64Payload_ThrowsInvalidDataException()
        {
            using var context = new StoreTestContext();

            await context.WriteEnvelopeAsync(formatVersion: 1, protectionMethod: "WindowsDpapiLocalMachine", payload: "This is not valid Base64 data.", TestContext.Current.CancellationToken);

            var exception = await Assert.ThrowsAsync<InvalidDataException>(async () => await context.Store.LoadAsync(TestContext.Current.CancellationToken));

            Assert.Contains("Base64", exception.Message, StringComparison.OrdinalIgnoreCase);

            Assert.IsType<FormatException>(exception.InnerException);

            Assert.Equal(0, context.ProtectionService.UnprotectDataCallCount);
        }

        [Fact]
        public async Task LoadAsync_WhenPayloadCannotBeUnprotected_PropagatesProtectionException()
        {
            var protectionException = new CryptographicException("The protected configuration belongs to another machine.");

            using var context = new StoreTestContext(unprotectDataException: protectionException);

            await context.WriteEnvelopeAsync(formatVersion: 1, protectionMethod: "WindowsDpapiLocalMachine", payload: Convert.ToBase64String([1, 2, 3]), TestContext.Current.CancellationToken);

            var actualException = await Assert.ThrowsAsync<CryptographicException>(async () => await context.Store.LoadAsync(TestContext.Current.CancellationToken));

            Assert.Same(protectionException, actualException);

            Assert.Equal(1, context.ProtectionService.UnprotectDataCallCount);
        }

        [Fact]
        public async Task LoadAsync_WhenUnprotectedConfigurationIsInvalidJson_ThrowsJsonException()
        {
            using var context = new StoreTestContext();

            var protectedPayload = context.ProtectionService.ProtectData(Encoding.UTF8.GetBytes("{ invalid configuration json"));

            await context.WriteEnvelopeAsync(formatVersion: 1, protectionMethod: "WindowsDpapiLocalMachine", payload: Convert.ToBase64String(protectedPayload), TestContext.Current.CancellationToken);

            var initialProtectCallCount = context.ProtectionService.ProtectDataCallCount;

            await Assert.ThrowsAsync<JsonException>(async () => await context.Store.LoadAsync(TestContext.Current.CancellationToken));

            Assert.Equal(initialProtectCallCount, context.ProtectionService.ProtectDataCallCount);

            Assert.Equal(1, context.ProtectionService.UnprotectDataCallCount);
        }

        [Fact]
        public async Task LoadAsync_WithCompletedProtectedConfigurationWithoutConnectionString_ThrowsInvalidDataException()
        {
            using var context = new StoreTestContext();

            var configuration = new ApplicationConfiguration
            {
                IsSetupComplete = true,
                SetupVersion = ApplicationConfiguration.CurrentSetupVersion,
                DatabaseConfiguration = new DatabaseConfiguration
                {
                    ConnectionString = string.Empty
                }
            };

            await context.WriteProtectedConfigurationAsync(configuration, TestContext.Current.CancellationToken);

            var exception = await Assert.ThrowsAsync<InvalidDataException>(async () => await context.Store.LoadAsync(TestContext.Current.CancellationToken));

            Assert.Contains("does not contain a database connection string", exception.Message, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(1, context.ProtectionService.UnprotectDataCallCount);
        }

        [Fact]
        public async Task LoadAsync_WithProtectedConfigurationUsingInvalidSetupVersion_ThrowsInvalidDataException()
        {
            using var context = new StoreTestContext();

            var configuration = new ApplicationConfiguration
            {
                IsSetupComplete = false,
                SetupVersion = 0
            };

            await context.WriteProtectedConfigurationAsync(configuration, TestContext.Current.CancellationToken);

            var exception = await Assert.ThrowsAsync<InvalidDataException>(async () => await context.Store.LoadAsync(TestContext.Current.CancellationToken));

            Assert.Contains("invalid setup version", exception.Message, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(1, context.ProtectionService.UnprotectDataCallCount);
        }

        [Fact]
        public async Task SaveAsync_WhenProtectionFails_CreatesNoConfigurationFileAndLeavesNoTemporaryFile()
        {
            var protectionException = new CryptographicException("The machine configuration could not be protected.");

            using var context = new StoreTestContext(protectDataException: protectionException);

            var actualException = await Assert.ThrowsAsync<CryptographicException>(async () => await context.Store.SaveAsync(CreateCompletedConfiguration(), TestContext.Current.CancellationToken));

            Assert.Same(protectionException, actualException);

            Assert.False(File.Exists(context.ConfigurationFilePath));

            Assert.Empty(Directory.GetFiles(context.ConfigurationDirectoryPath, "*.tmp", SearchOption.TopDirectoryOnly));

            Assert.Equal(1, context.ProtectionService.ProtectDataCallCount);

            Assert.Equal(1, context.ProtectionService.ProtectDirectoryCallCount);

            Assert.Equal(0, context.ProtectionService.ProtectFileCallCount);
        }

        private static ApplicationConfiguration CreateCompletedConfiguration() => new()
        {
            IsSetupComplete = true,
            SetupVersion = ApplicationConfiguration.CurrentSetupVersion,
            DatabaseConfiguration = new DatabaseConfiguration
            {
                ConnectionString = ConnectionString
            },
            RepositoryConfiguration = new RepositoryConfiguration
            {
                AutomaticallyRepairPaths = false
            },
            SecurityConfiguration = new SecurityConfiguration
            {
                AllowSelfDeletion = true,
                AllowSelfModification = false,
                HideOtherUsersFromRegularUsers = false
            }
        };

        private sealed class StoreTestContext : IDisposable
        {
            private static readonly JsonSerializerOptions SerializerOptions = new()
            {
                WriteIndented = true
            };

            private readonly string _rootPath = Path.Combine(Path.GetTempPath(), "MoprConfigurationTests", Guid.NewGuid().ToString("N"));

            public StoreTestContext(bool isElevatedAdministrator = true, Exception? protectDataException = null, Exception? unprotectDataException = null)
            {
                PathProvider = new MachineConfigurationPathProvider(_rootPath);

                ProtectionService = new RecordingProtectionService(protectDataException, unprotectDataException);

                Store = new ApplicationConfigurationStore(new TestAuthorizationService(isElevatedAdministrator), PathProvider, ProtectionService);
            }

            public string ConfigurationDirectoryPath => PathProvider.ConfigurationDirectoryPath;

            public string ConfigurationFilePath => PathProvider.ConfigurationFilePath;

            public MachineConfigurationPathProvider PathProvider { get; }

            public RecordingProtectionService ProtectionService { get; }

            public ApplicationConfigurationStore Store { get; }

            public void Dispose()
            {
                if (Directory.Exists(_rootPath))
                {
                    Directory.Delete(_rootPath, recursive: true);
                }
            }

            public async Task WriteEnvelopeAsync(int formatVersion, string protectionMethod, string payload, CancellationToken cancellationToken)
            {
                var envelope = new
                {
                    FormatVersion = formatVersion,
                    Payload = payload,
                    ProtectionMethod = protectionMethod
                };

                var content = JsonSerializer.Serialize(envelope, SerializerOptions);

                await WriteRawConfigurationFileAsync(content, cancellationToken);
            }

            public async Task WriteProtectedConfigurationAsync(ApplicationConfiguration configuration, CancellationToken cancellationToken)
            {
                var unprotectedData = JsonSerializer.SerializeToUtf8Bytes(configuration, SerializerOptions);

                var protectedData = ProtectionService.ProtectData(unprotectedData);

                try
                {
                    await WriteEnvelopeAsync(formatVersion: 1, protectionMethod: "WindowsDpapiLocalMachine", payload: Convert.ToBase64String(protectedData), cancellationToken);
                }
                finally
                {
                    Array.Clear(unprotectedData, 0, unprotectedData.Length);

                    Array.Clear(protectedData, 0, protectedData.Length);
                }
            }

            public async Task WriteRawConfigurationFileAsync(string content, CancellationToken cancellationToken)
            {
                Directory.CreateDirectory(ConfigurationDirectoryPath);

                await File.WriteAllTextAsync(ConfigurationFilePath, content, cancellationToken);
            }
        }

        private sealed class TestAuthorizationService(bool isElevatedAdministrator) : IAdministrativeAuthorizationService
        {
            public bool IsElevatedAdministrator { get; } = isElevatedAdministrator;

            public void DemandElevatedAdministrator()
            {
                if (!IsElevatedAdministrator)
                {
                    throw new UnauthorizedAccessException("Machine-wide MOPR configuration requires an elevated local administrator.");
                }
            }
        }

        internal sealed class RecordingProtectionService(Exception? protectDataException = null, Exception? unprotectDataException = null) : IMachineConfigurationProtectionService
        {
            private static readonly byte[] ProtectionPrefix = Encoding.UTF8.GetBytes("MOPR-TEST-PROTECTED:");

            private readonly Exception? _protectDataException = protectDataException;
            private readonly Exception? _unprotectDataException = unprotectDataException;

            public int ProtectDataCallCount { get; private set; }

            public int ProtectDirectoryCallCount { get; private set; }

            public int ProtectFileCallCount { get; private set; }

            public int UnprotectDataCallCount { get; private set; }

            public byte[] ProtectData(byte[] unprotectedData)
            {
                ArgumentNullException.ThrowIfNull(unprotectedData);

                ProtectDataCallCount++;

                if (_protectDataException is not null)
                {
                    throw _protectDataException;
                }

                var protectedData = new byte[ProtectionPrefix.Length + unprotectedData.Length];

                ProtectionPrefix.CopyTo(protectedData, 0);

                unprotectedData.CopyTo(protectedData, ProtectionPrefix.Length);

                return protectedData;
            }

            public void ProtectDirectory(string directoryPath) => ProtectDirectoryCallCount++;

            public void ProtectFile(string filePath) => ProtectFileCallCount++;

            public byte[] UnprotectData(byte[] protectedData)
            {
                ArgumentNullException.ThrowIfNull(protectedData);

                UnprotectDataCallCount++;

                if (_unprotectDataException is not null)
                {
                    throw _unprotectDataException;
                }

                if (protectedData.Length < ProtectionPrefix.Length || !protectedData.AsSpan(0, ProtectionPrefix.Length).SequenceEqual(ProtectionPrefix))
                {
                    throw new InvalidDataException("The test machine configuration payload is not protected.");
                }

                return protectedData[ProtectionPrefix.Length..];
            }
        }
    }
}