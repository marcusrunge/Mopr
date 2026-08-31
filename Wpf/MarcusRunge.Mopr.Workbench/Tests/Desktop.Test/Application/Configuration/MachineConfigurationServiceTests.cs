using MarcusRunge.Mopr.Workbench.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
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
            public int LoadCallCount { get; private set; }

            public int SaveCallCount { get; private set; }

            public IApplicationConfiguration? SavedConfiguration { get; private set; }

            /// <inheritdoc/>
            public string ConfigurationFilePath => "application.json";

            /// <inheritdoc/>
            public Task<IApplicationConfiguration> LoadAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadCallCount++;

                return Task.FromResult(configuration);
            }

            /// <inheritdoc/>
            public Task SaveAsync(IApplicationConfiguration configurationToSave, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SaveCallCount++;
                SavedConfiguration = configurationToSave;

                return Task.CompletedTask;
            }
        }
    }
}