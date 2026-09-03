using MarcusRunge.Mopr.Workbench.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Configuration
{
    public sealed class ApplicationConfigurationStoreTests
    {
        [Fact]
        public async Task LoadAsync_WhenConfigurationDoesNotExist_ReturnsIncompleteDefaults()
        {
            using var context = new StoreTestContext();

            var configuration = await context.Store.LoadAsync(TestContext.Current.CancellationToken);

            Assert.False(configuration.IsSetupComplete);
            Assert.Empty(configuration.Database.ConnectionString);
            Assert.Equal(ApplicationConfiguration.CurrentSetupVersion, configuration.SetupVersion);
        }

        [Fact]
        public async Task SaveAndLoadAsync_WithElevatedAdministrator_PreservesConfiguration()
        {
            using var context = new StoreTestContext(isElevatedAdministrator: true);
            var expected = new ApplicationConfiguration
            {
                IsSetupComplete = true,
                SetupVersion = ApplicationConfiguration.CurrentSetupVersion,
                DatabaseConfiguration = new DatabaseConfiguration
                {
                    ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=MoprTests;"
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

            await context.Store.SaveAsync(expected, TestContext.Current.CancellationToken);
            var actual = await context.Store.LoadAsync(TestContext.Current.CancellationToken);

            Assert.True(actual.IsSetupComplete);
            Assert.Equal(expected.SetupVersion, actual.SetupVersion);
            Assert.Equal(expected.Database.ConnectionString, actual.Database.ConnectionString);
            Assert.False(actual.Repository.AutomaticallyRepairPaths);
            Assert.True(actual.Security.AllowSelfDeletion);
            Assert.False(actual.Security.AllowSelfModification);
            Assert.False(actual.Security.HideOtherUsersFromRegularUsers);
            Assert.Equal(1, context.ProtectionService.ProtectDirectoryCallCount);
            Assert.Equal(1, context.ProtectionService.ProtectFileCallCount);
        }

        [Fact]
        public async Task SaveAsync_WithoutElevatedAdministrator_ThrowsAndCreatesNoFile()
        {
            using var context = new StoreTestContext(isElevatedAdministrator: false);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await context.Store.SaveAsync(new ApplicationConfiguration(), TestContext.Current.CancellationToken));

            Assert.False(File.Exists(context.ConfigurationFilePath));
            Assert.Equal(0, context.ProtectionService.ProtectDirectoryCallCount);
            Assert.Equal(0, context.ProtectionService.ProtectFileCallCount);
        }

        [Fact]
        public async Task LoadAsync_WithInvalidJson_ThrowsJsonException()
        {
            using var context = new StoreTestContext();
            Directory.CreateDirectory(context.ConfigurationDirectoryPath);

            await File.WriteAllTextAsync(context.ConfigurationFilePath, "{ invalid json", TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<System.Text.Json.JsonException>(async () => await context.Store.LoadAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task LoadAsync_WithCompletedConfigurationWithoutConnectionString_ThrowsInvalidDataException()
        {
            using var context = new StoreTestContext();
            Directory.CreateDirectory(context.ConfigurationDirectoryPath);

            await File.WriteAllTextAsync(context.ConfigurationFilePath, """
                                {
                
                  "isSetupComplete": true,
                  "setupVersion": 1,
                  "databaseConfiguration": {
                    "connectionString": ""
                  }
                }
                """, TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<InvalidDataException>(async () => await context.Store.LoadAsync(TestContext.Current.CancellationToken));
        }

        private sealed class StoreTestContext : IDisposable
        {
            private readonly string _rootPath = Path.Combine(Path.GetTempPath(), "MoprConfigurationTests", Guid.NewGuid().ToString("N"));

            public StoreTestContext(bool isElevatedAdministrator = true)
            {
                PathProvider = new MachineConfigurationPathProvider(_rootPath);
                ProtectionService = new RecordingProtectionService();

                Store = new ApplicationConfigurationStore(
                    new TestAuthorizationService(isElevatedAdministrator),
                    PathProvider,
                    ProtectionService);
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
        }

        private sealed class TestAuthorizationService(bool isElevatedAdministrator)
            : IAdministrativeAuthorizationService
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

        internal sealed class RecordingProtectionService : IMachineConfigurationProtectionService
        {
            private static readonly byte[] ProtectionPrefix = Encoding.UTF8.GetBytes("MOPR-TEST-PROTECTED:");

            public int ProtectDataCallCount { get; private set; }

            public int ProtectDirectoryCallCount { get; private set; }

            public int ProtectFileCallCount { get; private set; }

            public int UnprotectDataCallCount { get; private set; }

            public byte[] ProtectData(byte[] unprotectedData)
            {
                ArgumentNullException.ThrowIfNull(unprotectedData);

                ProtectDataCallCount++;

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

                if (protectedData.Length < ProtectionPrefix.Length || !protectedData.AsSpan(0, ProtectionPrefix.Length).SequenceEqual(ProtectionPrefix))
                {
                    throw new InvalidDataException("The test machine configuration payload is not protected.");
                }

                return protectedData[ProtectionPrefix.Length..];
            }
        }
    }
}