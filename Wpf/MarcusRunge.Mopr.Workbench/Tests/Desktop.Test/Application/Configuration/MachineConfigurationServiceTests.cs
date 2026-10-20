using MarcusRunge.Mopr.Workbench.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Configuration
{
    public sealed class MachineConfigurationServiceTests
    {
        [Fact]
        public void CanModify_WhenProcessIsElevated_ReturnsTrue()
        {
            var context = new MachineConfigurationServiceTestContext(isElevatedAdministrator: true);

            Assert.True(context.Service.CanModify);
        }

        [Fact]
        public void CanModify_WhenProcessIsNotElevated_ReturnsFalse()
        {
            var context = new MachineConfigurationServiceTestContext(isElevatedAdministrator: false);

            Assert.False(context.Service.CanModify);
        }

        [Fact]
        public async Task LoadAsync_ReturnsConfigurationFromStore()
        {
            var expected = CreateValidConfiguration();
            var context = new MachineConfigurationServiceTestContext(configuration: expected);

            var actual = await context.Service.LoadAsync(TestContext.Current.CancellationToken);

            Assert.Same(expected, actual);
            Assert.Equal(1, context.Store.LoadCallCount);
        }

        [Fact]
        public void ValidateForSetupCompletion_WithValidConfiguration_ReturnsSuccess()
        {
            var context = new MachineConfigurationServiceTestContext();

            var result = context.Service.ValidateForSetupCompletion(CreateValidConfiguration());

            Assert.True(result.IsValid);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public void ValidateForSetupCompletion_WithIncompleteConfiguration_ReturnsAllIssues()
        {
            var context = new MachineConfigurationServiceTestContext();
            var configuration = new ApplicationConfiguration
            {
                DatabaseConfiguration = new DatabaseConfiguration
                {
                    ConnectionString = " "
                },
                IsSetupComplete = false,
                SetupVersion = 0
            };

            var result = context.Service.ValidateForSetupCompletion(configuration);

            Assert.False(result.IsValid);
            Assert.Contains(MachineConfigurationIssue.InvalidSetupVersion, result.Issues);
            Assert.Contains(MachineConfigurationIssue.DatabaseConnectionStringMissing, result.Issues);
            Assert.Contains(MachineConfigurationIssue.SetupNotCompleted, result.Issues);
        }

        [Fact]
        public async Task SaveAsync_WithValidConfiguration_SavesThroughStore()
        {
            var configuration = CreateValidConfiguration();
            var context = new MachineConfigurationServiceTestContext(isElevatedAdministrator: true);

            await context.Service.SaveAsync(configuration, TestContext.Current.CancellationToken);

            Assert.Same(configuration, context.Store.SavedConfiguration);
            Assert.Equal(1, context.AuthorizationService.DemandCallCount);
            Assert.Equal(1, context.Store.SaveCallCount);
        }

        [Fact]
        public async Task SaveAsync_WithInvalidConfiguration_ThrowsBeforeAuthorizationAndStorage()
        {
            var context = new MachineConfigurationServiceTestContext(isElevatedAdministrator: true);
            var configuration = new ApplicationConfiguration();

            var exception = await Assert.ThrowsAsync<MachineConfigurationValidationException>(async () => await context.Service.SaveAsync(configuration, TestContext.Current.CancellationToken));

            Assert.False(exception.ValidationResult.IsValid);
            Assert.Contains(MachineConfigurationIssue.DatabaseConnectionStringMissing, exception.ValidationResult.Issues);
            Assert.Contains(MachineConfigurationIssue.SetupNotCompleted, exception.ValidationResult.Issues);
            Assert.Equal(0, context.AuthorizationService.DemandCallCount);
            Assert.Equal(0, context.Store.SaveCallCount);
        }

        [Fact]
        public async Task SaveAsync_WithoutElevation_ThrowsBeforeStorage()
        {
            var context = new MachineConfigurationServiceTestContext(isElevatedAdministrator: false);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await context.Service.SaveAsync(CreateValidConfiguration(), TestContext.Current.CancellationToken));

            Assert.Equal(1, context.AuthorizationService.DemandCallCount);
            Assert.Equal(0, context.Store.SaveCallCount);
        }

        [Fact]
        public async Task UpdateSecurityAsync_WhenRequestIsValid_PreservesOtherConfigurationAndSavesSecurityValues()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var configuration = CreateValidConfiguration();
            var context = new MachineConfigurationServiceTestContext(isElevatedAdministrator: true, configuration);
            var request = new SecurityConfigurationUpdateRequest(AllowSelfDeletion: true, AllowSelfModification: false, HideOtherUsersFromRegularUsers: true);

            var result = await context.Service.UpdateSecurityAsync(request, cancellationToken);

            Assert.True(result.IsSuccess);
            Assert.NotNull(context.Store.SavedConfiguration);
            Assert.Same(configuration.Database, context.Store.SavedConfiguration.Database);
            Assert.Same(configuration.Repository, context.Store.SavedConfiguration.Repository);
            Assert.Equal(configuration.IsSetupComplete, context.Store.SavedConfiguration.IsSetupComplete);
            Assert.Equal(configuration.SetupVersion, context.Store.SavedConfiguration.SetupVersion);
            Assert.True(context.Store.SavedConfiguration.Security.AllowSelfDeletion);
            Assert.False(context.Store.SavedConfiguration.Security.AllowSelfModification);
            Assert.True(context.Store.SavedConfiguration.Security.HideOtherUsersFromRegularUsers);
            Assert.Equal(1, context.Store.LoadCallCount);
            Assert.Equal(1, context.Store.SaveCallCount);
            Assert.Equal(1, context.AuthorizationService.DemandCallCount);
        }

        [Fact]
        public async Task UpdateSecurityAsync_WithoutElevation_ReturnsUnauthorizedWithoutLoadingOrSaving()
        {
            var context = new MachineConfigurationServiceTestContext(isElevatedAdministrator: false, CreateValidConfiguration());
            var result = await context.Service.UpdateSecurityAsync(new(false, false, false), TestContext.Current.CancellationToken);
            Assert.Equal(SecurityConfigurationUpdateStatus.Unauthorized, result.Status);
            Assert.Equal(0, context.Store.LoadCallCount);
            Assert.Equal(0, context.Store.SaveCallCount);
        }

        [Fact]
        public async Task UpdateSecurityAsync_WhenCurrentConfigurationIsInvalid_ReturnsValidationFailed()
        {
            var context = new MachineConfigurationServiceTestContext(isElevatedAdministrator: true, new ApplicationConfiguration());
            var result = await context.Service.UpdateSecurityAsync(new(false, false, false), TestContext.Current.CancellationToken);
            Assert.Equal(SecurityConfigurationUpdateStatus.ValidationFailed, result.Status);
            Assert.Equal(1, context.Store.LoadCallCount);
            Assert.Equal(0, context.Store.SaveCallCount);
            Assert.Equal(0, context.AuthorizationService.DemandCallCount);
        }

        [Fact]
        public async Task UpdateSecurityAsync_WhenLoadFails_ReturnsConfigurationUnavailable()
        {
            var context = new MachineConfigurationServiceTestContext(configuration: CreateValidConfiguration());
            context.Store.LoadException = new InvalidOperationException("Load failed.");
            var result = await context.Service.UpdateSecurityAsync(new(false, false, false), TestContext.Current.CancellationToken);
            Assert.Equal(SecurityConfigurationUpdateStatus.ConfigurationUnavailable, result.Status);
            Assert.Equal(0, context.Store.SaveCallCount);
        }

        [Fact]
        public async Task UpdateSecurityAsync_WhenSaveFails_ReturnsFailed()
        {
            var context = new MachineConfigurationServiceTestContext(configuration: CreateValidConfiguration());
            context.Store.SaveException = new InvalidOperationException("Save failed.");
            var result = await context.Service.UpdateSecurityAsync(new(false, false, false), TestContext.Current.CancellationToken);
            Assert.Equal(SecurityConfigurationUpdateStatus.Failed, result.Status);
            Assert.Equal(1, context.Store.SaveCallCount);
        }

        [Fact]
        public async Task UpdateSecurityAsync_WhenCanceled_PropagatesCancellation()
        {
            using var cancellationSource = new CancellationTokenSource();
            cancellationSource.Cancel();
            var context = new MachineConfigurationServiceTestContext(configuration: CreateValidConfiguration());
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await context.Service.UpdateSecurityAsync(new(false, false, false), cancellationSource.Token));
        }

        [Fact]
        public async Task TestDatabaseConnectionAsync_WhenPersistenceSucceeds_ReturnsTrue()
        {
            var context = new MachineConfigurationServiceTestContext();
            var configuration = new DatabaseConfiguration
            {
                ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=MoprDb;"
            };

            context.Persistence.Setup(persistence => persistence.TestConnectionAsync(It.Is<PersistenceConfiguration>(candidate => candidate.ConnectionString == configuration.ConnectionString && candidate.Mode == PersistenceMode.SqlServer), It.IsAny<CancellationToken>())).ReturnsAsync(new PersistenceConnectionTestResult
            {
                IsSuccessful = true,
                Message = "Connection successful."
            });

            var result = await context.Service.TestDatabaseConnectionAsync(configuration, TestContext.Current.CancellationToken);

            Assert.True(result);

            context.Persistence.Verify(persistence => persistence.TestConnectionAsync(It.IsAny<PersistenceConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TestDatabaseConnectionAsync_WhenPersistenceFails_ReturnsFalse()
        {
            var context = new MachineConfigurationServiceTestContext();

            context.Persistence.Setup(persistence => persistence.TestConnectionAsync(It.IsAny<PersistenceConfiguration>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PersistenceConnectionTestResult
                {
                    IsSuccessful = false,
                    Message = "Technical test failure.",
                    Exception = new InvalidOperationException("Technical test failure.")
                });

            var result = await context.Service.TestDatabaseConnectionAsync(new DatabaseConfiguration
            {
                ConnectionString = "Invalid"
            }, TestContext.Current.CancellationToken);

            Assert.False(result);
        }

        [Fact]
        public async Task TestDatabaseConnectionAsync_WhenCanceled_PropagatesCancellation()
        {
            var context = new MachineConfigurationServiceTestContext();

            context.Persistence.Setup(persistence => persistence.TestConnectionAsync(It.IsAny<PersistenceConfiguration>(), It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await context.Service.TestDatabaseConnectionAsync(new DatabaseConfiguration
            {
                ConnectionString = "Canceled"
            }, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task TestDatabaseConnectionAsync_WithoutConfiguration_ThrowsArgumentNullException()
        {
            var context = new MachineConfigurationServiceTestContext();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await context.Service.TestDatabaseConnectionAsync(null!, TestContext.Current.CancellationToken));
        }

        private static ApplicationConfiguration CreateValidConfiguration() => new()
        {
            DatabaseConfiguration = new DatabaseConfiguration
            {
                ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=MoprDb;"
            },
            IsSetupComplete = true,
            SetupVersion = ApplicationConfiguration.CurrentSetupVersion
        };

        private sealed class MachineConfigurationServiceTestContext
        {
            public MachineConfigurationServiceTestContext(bool isElevatedAdministrator = true, IApplicationConfiguration? configuration = null)
            {
                AuthorizationService = new TestAuthorizationService(isElevatedAdministrator);

                Store = new TestConfigurationStore(configuration ?? new ApplicationConfiguration());

                Persistence = new Mock<IPersistence>(MockBehavior.Strict);

                Service = new MachineConfigurationService(AuthorizationService, Store, Persistence.Object);
            }

            public TestAuthorizationService AuthorizationService { get; }

            public Mock<IPersistence> Persistence { get; }

            public MachineConfigurationService Service { get; }

            public TestConfigurationStore Store { get; }
        }

        private sealed class TestAuthorizationService(bool isElevatedAdministrator) : IAdministrativeAuthorizationService
        {
            public int DemandCallCount { get; private set; }

            /// <inheritdoc/>
            public bool IsElevatedAdministrator { get; } = isElevatedAdministrator;

            /// <inheritdoc/>
            public void DemandElevatedAdministrator()
            {
                DemandCallCount++;

                if (!IsElevatedAdministrator)
                {
                    throw new UnauthorizedAccessException("Machine-wide MOPR configuration requires an elevated local administrator.");
                }
            }
        }

        private sealed class TestConfigurationStore(IApplicationConfiguration configuration) : IApplicationConfigurationStore
        {
            public Exception? LoadException { get; set; }

            public int LoadCallCount { get; private set; }

            public int SaveCallCount { get; private set; }

            public Exception? SaveException { get; set; }

            public IApplicationConfiguration? SavedConfiguration { get; private set; }

            /// <inheritdoc/>
            public string ConfigurationFilePath => "application.json";

            /// <inheritdoc/>
            public Task<IApplicationConfiguration> LoadAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadCallCount++;
                if (LoadException is not null) throw LoadException;
                return Task.FromResult(configuration);
            }

            /// <inheritdoc/>
            public Task SaveAsync(IApplicationConfiguration configurationToSave, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SaveCallCount++;
                if (SaveException is not null) throw SaveException;
                SavedConfiguration = configurationToSave;
                return Task.CompletedTask;
            }
        }
    }
}