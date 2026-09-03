using MarcusRunge.Mopr.Workbench.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Test.Application.Configuration
{
    public sealed class SetupCompletionServiceTests
    {
        [Fact]
        public async Task CompleteAsync_WithValidConfiguration_CompletesSetupAndPublishesConfiguration()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupCompletionServiceTestContext();

            var result = await context.Service.CompleteAsync(context.CreateRequest(), cancellationToken);

            Assert.True(result.IsSuccessful);
            Assert.Equal(SetupCompletionStatus.Completed, result.Status);
            Assert.False(result.RollbackAttempted);
            Assert.True(result.RollbackSuccessful);

            var item = Assert.Single(context.RepositoryLocationRepository.Locations);

            var repositoryLocation = item;

            Assert.Equal(context.NormalizedRepositoryPath, repositoryLocation.RootPath);
            Assert.Equal(SetupCompletionServiceTestContext.AuditUserId, repositoryLocation.CreatedByUserId);
            Assert.Equal("Default DICOM repository", repositoryLocation.Name);
            Assert.True(repositoryLocation.IsEnabled);
            Assert.True(repositoryLocation.IsDefault);

            Assert.NotNull(context.SavedConfiguration);
            Assert.True(context.SavedConfiguration.IsSetupComplete);
            Assert.Equal(context.ConnectionString, context.SavedConfiguration.Database.ConnectionString);

            Assert.True(context.ApplicationConfigurationSubject.Value.IsSetupComplete);
            Assert.Equal(context.ConnectionString, context.PersistenceConfigurationSubject.Value.ConnectionString);
            Assert.Equal(PersistenceMode.SqlServer, context.PersistenceConfigurationSubject.Value.Mode);

            context.MachineConfigurationService.Verify(service => service.TestDatabaseConnectionAsync(It.Is<IDatabaseConfiguration>(configuration => configuration.ConnectionString == context.ConnectionString), cancellationToken), Times.Once);

            context.MachineConfigurationService.Verify(service => service.SaveAsync(It.Is<IApplicationConfiguration>(configuration => configuration.IsSetupComplete), cancellationToken), Times.Once);

            context.RepositoryLocationValidationService.Verify(service => service.ValidateAsync(context.RepositoryPath, cancellationToken), Times.Once);

            context.AuditIdentityProvider.Verify(provider => provider.GetOrCreateUserIdAsync(cancellationToken), Times.Once);
        }

        [Fact]
        public async Task CompleteAsync_WhenDatabaseValidationFails_DoesNotInitializePersistenceOrModifyConfiguration()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupCompletionServiceTestContext(databaseValid: false);

            var result = await context.Service.CompleteAsync(context.CreateRequest(), cancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Equal(SetupCompletionStatus.DatabaseValidationFailed, result.Status);
            Assert.False(result.RollbackAttempted);
            Assert.True(result.RollbackSuccessful);
            Assert.Empty(context.RepositoryLocationRepository.Locations);
            Assert.Null(context.SavedConfiguration);
            Assert.False(context.ApplicationConfigurationSubject.Value.IsSetupComplete);

            context.RepositoryLocationValidationService.Verify(
                service => service.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);

            context.AuditIdentityProvider.Verify(provider => provider.GetOrCreateUserIdAsync(It.IsAny<CancellationToken>()), Times.Never);

            context.MachineConfigurationService.Verify(service => service.SaveAsync(It.IsAny<IApplicationConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteAsync_WhenRepositoryValidationFails_DoesNotInitializePersistenceOrSaveConfiguration()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new SetupCompletionServiceTestContext(repositoryValid: false);

            var result = await context.Service.CompleteAsync(context.CreateRequest(), cancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Equal(SetupCompletionStatus.RepositoryValidationFailed, result.Status);
            Assert.False(result.RollbackAttempted);
            Assert.True(result.RollbackSuccessful);
            Assert.Empty(context.RepositoryLocationRepository.Locations);
            Assert.Null(context.SavedConfiguration);
            Assert.False(context.ApplicationConfigurationSubject.Value.IsSetupComplete);

            context.AuditIdentityProvider.Verify(provider => provider.GetOrCreateUserIdAsync(It.IsAny<CancellationToken>()), Times.Never);

            context.MachineConfigurationService.Verify(service => service.SaveAsync(It.IsAny<IApplicationConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteAsync_WhenRepositoryLocationAlreadyExists_ReusesLocationWithoutCreatingDuplicate()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var existingLocation = new RepositoryLocation
            {
                Id = 17,
                CreatedByUserId = 3,
                IsDefault = true,
                IsEnabled = true,
                Name = "Existing repository",
                RootPath = SetupCompletionServiceTestContext.DefaultNormalizedRepositoryPath
            };

            var context = new SetupCompletionServiceTestContext(existingLocation);

            var result = await context.Service.CompleteAsync(context.CreateRequest(), cancellationToken);

            Assert.True(result.IsSuccessful);
            var item = Assert.Single(context.RepositoryLocationRepository.Locations);
            Assert.Same(existingLocation, item);
            Assert.Equal(0, context.RepositoryLocationRepository.AddCount);
            Assert.Equal(0, context.RepositoryLocationRepository.UpdateCount);
            Assert.Equal(0, context.RepositoryLocationRepository.DeleteCount);
        }

        [Fact]
        public async Task CompleteAsync_WhenExistingLocationIsDisabled_ActivatesAndMakesLocationDefault()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var existingLocation = new RepositoryLocation
            {
                Id = 19,
                CreatedByUserId = 3,
                IsDefault = false,
                IsEnabled = false,
                Name = "Existing repository",
                RootPath = SetupCompletionServiceTestContext.DefaultNormalizedRepositoryPath
            };

            var context = new SetupCompletionServiceTestContext(existingLocation);

            var result = await context.Service.CompleteAsync(context.CreateRequest(), cancellationToken);

            Assert.True(result.IsSuccessful);
            Assert.True(existingLocation.IsEnabled);
            Assert.True(existingLocation.IsDefault);
            Assert.Equal(SetupCompletionServiceTestContext.AuditUserId, existingLocation.ModifiedByUserId);
            Assert.Equal(0, context.RepositoryLocationRepository.AddCount);
            Assert.Equal(1, context.RepositoryLocationRepository.UpdateCount);
            Assert.Equal(0, context.RepositoryLocationRepository.DeleteCount);
        }

        [Fact]
        public async Task CompleteAsync_WhenMachineConfigurationSaveFails_RemovesCreatedRepositoryLocation()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var saveException = new IOException("The machine configuration could not be saved.");
            var context = new SetupCompletionServiceTestContext(saveException: saveException);

            var result = await context.Service.CompleteAsync(context.CreateRequest(), cancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Equal(SetupCompletionStatus.Failed, result.Status);
            Assert.True(result.RollbackAttempted);
            Assert.True(result.RollbackSuccessful);
            Assert.Contains(saveException.Message, result.TechnicalDetails, StringComparison.Ordinal);
            Assert.Empty(context.RepositoryLocationRepository.Locations);
            Assert.Equal(1, context.RepositoryLocationRepository.AddCount);
            Assert.Equal(1, context.RepositoryLocationRepository.DeleteCount);
            Assert.False(context.ApplicationConfigurationSubject.Value.IsSetupComplete);
        }

        [Fact]
        public async Task CompleteAsync_WhenMachineConfigurationSaveFails_RestoresExistingRepositoryLocation()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var existingLocation = new RepositoryLocation
            {
                Id = 23,
                CreatedByUserId = 3,
                IsDefault = false,
                IsEnabled = false,
                ModifiedAtUtc = null,
                ModifiedByUserId = null,
                Name = "Existing repository",
                RootPath = SetupCompletionServiceTestContext.DefaultNormalizedRepositoryPath
            };

            var context = new SetupCompletionServiceTestContext(existingLocation, saveException: new IOException("The machine configuration could not be saved."));

            var result = await context.Service.CompleteAsync(context.CreateRequest(), cancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Equal(SetupCompletionStatus.Failed, result.Status);
            Assert.True(result.RollbackAttempted);
            Assert.True(result.RollbackSuccessful);
            Assert.False(existingLocation.IsEnabled);
            Assert.False(existingLocation.IsDefault);
            Assert.Null(existingLocation.ModifiedAtUtc);
            Assert.Null(existingLocation.ModifiedByUserId);
            Assert.Equal("Existing repository", existingLocation.Name);
            Assert.Equal(2, context.RepositoryLocationRepository.UpdateCount);
            Assert.Equal(0, context.RepositoryLocationRepository.DeleteCount);
            Assert.False(context.ApplicationConfigurationSubject.Value.IsSetupComplete);
        }

        [Fact]
        public async Task CompleteAsync_WhenMachineConfigurationSaveFails_RestoresPreviousDefaultLocation()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            var selectedLocation = new RepositoryLocation
            {
                Id = 29,
                CreatedByUserId = 3,
                IsDefault = false,
                IsEnabled = true,
                Name = "Selected repository",
                RootPath = SetupCompletionServiceTestContext.DefaultNormalizedRepositoryPath
            };

            var previousDefault = new RepositoryLocation
            {
                Id = 31,
                CreatedByUserId = 3,
                IsDefault = true,
                IsEnabled = true,
                Name = "Previous repository",
                RootPath = @"D:\PreviousRepository"
            };

            var context = new SetupCompletionServiceTestContext(
                [selectedLocation, previousDefault],
                saveException: new IOException("The machine configuration could not be saved."));

            var result = await context.Service.CompleteAsync(context.CreateRequest(), cancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Equal(SetupCompletionStatus.Failed, result.Status);
            Assert.True(result.RollbackAttempted);
            Assert.True(result.RollbackSuccessful);
            Assert.False(selectedLocation.IsDefault);
            Assert.True(selectedLocation.IsEnabled);
            Assert.True(previousDefault.IsDefault);
            Assert.True(previousDefault.IsEnabled);
            Assert.Equal(3, context.RepositoryLocationRepository.UpdateCount);
            Assert.False(context.ApplicationConfigurationSubject.Value.IsSetupComplete);
        }

        [Fact]
        public async Task CompleteAsync_WhenRollbackFails_ReturnsFailedAndRollbackFailed()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var saveException = new IOException("The machine configuration could not be saved.");
            var rollbackException = new InvalidOperationException("The repository location could not be removed.");

            var context = new SetupCompletionServiceTestContext(saveException: saveException, deleteException: rollbackException);

            var result = await context.Service.CompleteAsync(context.CreateRequest(), cancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Equal(SetupCompletionStatus.FailedAndRollbackFailed, result.Status);
            Assert.True(result.RollbackAttempted);
            Assert.False(result.RollbackSuccessful);
            Assert.Contains(saveException.Message, result.TechnicalDetails, StringComparison.Ordinal);
            Assert.Contains(rollbackException.Message, result.TechnicalDetails, StringComparison.Ordinal);
            Assert.Single(context.RepositoryLocationRepository.Locations);
            Assert.False(context.ApplicationConfigurationSubject.Value.IsSetupComplete);
        }

        [Fact]
        public async Task CompleteAsync_WhenCanceledBeforeExecution_DoesNotAccessDependencies()
        {
            var context = new SetupCompletionServiceTestContext();
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

            cancellationSource.Cancel();

            var result = await context.Service.CompleteAsync(context.CreateRequest(), cancellationSource.Token);

            Assert.False(result.IsSuccessful);
            Assert.Equal(SetupCompletionStatus.Canceled, result.Status);
            Assert.False(result.RollbackAttempted);
            Assert.True(result.RollbackSuccessful);
            Assert.Empty(context.RepositoryLocationRepository.Locations);
            Assert.Null(context.SavedConfiguration);

            context.MachineConfigurationService.Verify(service => service.TestDatabaseConnectionAsync(It.IsAny<IDatabaseConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);

            context.RepositoryLocationValidationService.Verify(service => service.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteAsync_WhenCanceledAfterRepositoryLocationCreation_RollsBackCreatedLocation()
        {
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            var context = new SetupCompletionServiceTestContext
            {
                BeforeMachineConfigurationSave = () => cancellationSource.Cancel()
            };

            var result = await context.Service.CompleteAsync(context.CreateRequest(), cancellationSource.Token);

            Assert.False(result.IsSuccessful);
            Assert.Equal(SetupCompletionStatus.Canceled, result.Status);
            Assert.True(result.RollbackAttempted);
            Assert.True(result.RollbackSuccessful);
            Assert.Empty(context.RepositoryLocationRepository.Locations);
            Assert.Equal(1, context.RepositoryLocationRepository.AddCount);
            Assert.Equal(1, context.RepositoryLocationRepository.DeleteCount);
            Assert.Null(context.SavedConfiguration);
            Assert.False(context.ApplicationConfigurationSubject.Value.IsSetupComplete);
        }

        [Fact]
        public async Task CompleteAsync_WhenPersistenceInitializationFails_DoesNotAccessRepositoryContracts()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var initializationException = new InvalidOperationException("Persistence initialization failed.");
            var context = new SetupCompletionServiceTestContext(persistenceInitialization: Task.FromException(initializationException));

            var result = await context.Service.CompleteAsync(context.CreateRequest(), cancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Equal(SetupCompletionStatus.Failed, result.Status);
            Assert.False(result.RollbackAttempted);
            Assert.True(result.RollbackSuccessful);
            Assert.Contains(initializationException.Message, result.TechnicalDetails, StringComparison.Ordinal);
            Assert.Empty(context.RepositoryLocationRepository.Locations);
            Assert.Null(context.SavedConfiguration);

            context.Persistence.VerifyGet(persistence => persistence.RepositoryLocation, Times.Never);

            context.AuditIdentityProvider.Verify(provider => provider.GetOrCreateUserIdAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        private sealed class SetupCompletionServiceTestContext
        {
            public const int AuditUserId = 7;
            public const string DefaultNormalizedRepositoryPath = @"C:\MOPR\Repository";

            public SetupCompletionServiceTestContext(RepositoryLocation? existingLocation = null, bool databaseValid = true, bool repositoryValid = true, Exception? saveException = null, Exception? deleteException = null, Task? persistenceInitialization = null) : this(existingLocation is null ? [] : [existingLocation], databaseValid, repositoryValid, saveException, deleteException, persistenceInitialization)
            {
            }

            public SetupCompletionServiceTestContext(IEnumerable<RepositoryLocation> existingLocations, bool databaseValid = true, bool repositoryValid = true, Exception? saveException = null, Exception? deleteException = null, Task? persistenceInitialization = null)
            {
                RepositoryLocationRepository = new TestRepositoryLocationRepository(existingLocations, deleteException);

                Persistence.SetupGet(instance => instance.Initialization).Returns(persistenceInitialization ?? Task.CompletedTask);

                Persistence.SetupGet(instance => instance.RepositoryLocation).Returns(RepositoryLocationRepository);

                MachineConfigurationService.Setup(service => service.TestDatabaseConnectionAsync(It.IsAny<IDatabaseConfiguration>(), It.IsAny<CancellationToken>())).ReturnsAsync(databaseValid);

                MachineConfigurationService.Setup(service => service.SaveAsync(It.IsAny<IApplicationConfiguration>(), It.IsAny<CancellationToken>())).Returns<IApplicationConfiguration, CancellationToken>((configuration, cancellationToken) =>
                {
                    BeforeMachineConfigurationSave?.Invoke();
                    cancellationToken.ThrowIfCancellationRequested();

                    if (saveException is not null)
                    {
                        return Task.FromException(saveException);
                    }

                    SavedConfiguration = configuration;
                    return Task.CompletedTask;
                });

                RepositoryLocationValidationService.Setup(service => service.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new RepositoryLocationValidationResult
                {
                    Exists = repositoryValid,
                    IsReadable = repositoryValid,
                    IsWritable = repositoryValid,
                    NormalizedPath = repositoryValid ? NormalizedRepositoryPath : RepositoryPath
                });

                AuditIdentityProvider.Setup(provider => provider.GetOrCreateUserIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(AuditUserId);

                var initialApplicationConfiguration = CreateApplicationConfiguration(isSetupComplete: false);

                ApplicationConfigurationSubject = new BehaviorSubject<IApplicationConfiguration>(initialApplicationConfiguration);
                PersistenceConfigurationSubject = new BehaviorSubject<PersistenceConfiguration>(new PersistenceConfiguration());

                Service = new SetupCompletionService(MachineConfigurationService.Object, RepositoryLocationValidationService.Object, Persistence.Object, AuditIdentityProvider.Object, PersistenceConfigurationSubject, ApplicationConfigurationSubject);
            }

            public Mock<ISetupAuditIdentityProvider> AuditIdentityProvider { get; } = new(MockBehavior.Strict);

            public BehaviorSubject<IApplicationConfiguration> ApplicationConfigurationSubject { get; }

            public Action? BeforeMachineConfigurationSave { get; set; }

            public string ConnectionString { get; } = @"Server=(localdb)\MSSQLLocalDB;Database=MoprSetupTest;";

            public Mock<IMachineConfigurationService> MachineConfigurationService { get; } = new(MockBehavior.Strict);

            public string NormalizedRepositoryPath { get; } = DefaultNormalizedRepositoryPath;

            public Mock<IPersistence> Persistence { get; } = new(MockBehavior.Strict);

            public BehaviorSubject<PersistenceConfiguration> PersistenceConfigurationSubject { get; }

            public string RepositoryPath { get; } = DefaultNormalizedRepositoryPath + Path.DirectorySeparatorChar;

            public TestRepositoryLocationRepository RepositoryLocationRepository { get; }

            public Mock<IRepositoryLocationValidationService> RepositoryLocationValidationService { get; } = new(MockBehavior.Strict);

            public IApplicationConfiguration? SavedConfiguration { get; private set; }

            public SetupCompletionService Service { get; }

            public SetupCompletionRequest CreateRequest() => new(CreateApplicationConfiguration(isSetupComplete: false), RepositoryPath);

            private ApplicationConfiguration CreateApplicationConfiguration(bool isSetupComplete) => new()
            {
                DatabaseConfiguration = new DatabaseConfiguration
                {
                    ConnectionString = ConnectionString
                },
                IsSetupComplete = isSetupComplete,
                RepositoryConfiguration = new RepositoryConfiguration
                {
                    AutomaticallyRepairPaths = true
                },
                SecurityConfiguration = new SecurityConfiguration
                {
                    AllowSelfDeletion = false,
                    AllowSelfModification = true,
                    HideOtherUsersFromRegularUsers = true
                },
                SetupVersion = ApplicationConfiguration.CurrentSetupVersion
            };
        }

        private sealed class TestRepositoryLocationRepository : IRepositoryLocationRepository
        {
            private readonly Exception? _deleteException;
            private int _nextId;

            public TestRepositoryLocationRepository(IEnumerable<RepositoryLocation> locations, Exception? deleteException)
            {
                _deleteException = deleteException;
                Locations.AddRange(locations);
                _nextId = Locations.Count == 0 ? 1 : Locations.Max(location => location.Id) + 1;
            }

            public int AddCount { get; private set; }

            public int DeleteCount { get; private set; }

            public List<RepositoryLocation> Locations { get; } = [];

            public int UpdateCount { get; private set; }

            public Task AddAsync(RepositoryLocation entity, CancellationToken cancellationToken = default)
            {
                ArgumentNullException.ThrowIfNull(entity);
                cancellationToken.ThrowIfCancellationRequested();

                if (entity.IsDefault)
                {
                    ClearOtherDefaults(entity.Id);
                }

                if (entity.Id <= 0)
                {
                    entity.Id = _nextId++;
                }

                Locations.Add(entity);
                AddCount++;

                return Task.CompletedTask;
            }

            public Task DeleteAsync(RepositoryLocation entity, CancellationToken cancellationToken = default)
            {
                ArgumentNullException.ThrowIfNull(entity);
                cancellationToken.ThrowIfCancellationRequested();
                DeleteCount++;

                if (_deleteException is not null)
                {
                    return Task.FromException(_deleteException);
                }

                Locations.RemoveAll(location => location.Id == entity.Id);
                return Task.CompletedTask;
            }

            public Task<IList<RepositoryLocation>> GetAllAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult<IList<RepositoryLocation>>([.. Locations]);
            }

            public Task<RepositoryLocation?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(Locations.FirstOrDefault(location => location.Id == id));
            }

            public Task<RepositoryLocation?> GetByRootPathAsync(string rootPath, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                return Task.FromResult(Locations.FirstOrDefault(location => string.Equals(location.RootPath, rootPath, StringComparison.OrdinalIgnoreCase)));
            }

            public Task<RepositoryLocation?> GetDefaultAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(Locations.SingleOrDefault(location => location.IsDefault));
            }

            public Task<IList<RepositoryLocation>> GetEnabledAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                return Task.FromResult<IList<RepositoryLocation>>([.. Locations.Where(location => location.IsEnabled)]);
            }

            public Task UpdateAsync(RepositoryLocation entity, CancellationToken cancellationToken = default)
            {
                ArgumentNullException.ThrowIfNull(entity);
                cancellationToken.ThrowIfCancellationRequested();

                if (entity.IsDefault)
                {
                    ClearOtherDefaults(entity.Id);
                }

                UpdateCount++;

                return Task.CompletedTask;
            }

            private void ClearOtherDefaults(int selectedLocationId)
            {
                foreach (var location in Locations.Where(location => location.Id != selectedLocationId))
                {
                    location.IsDefault = false;
                }
            }
        }
    }
}