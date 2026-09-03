using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Bases;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Reactive.Subjects;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Test
{
    public sealed class PersistenceBaseTests
    {
        [Fact]
        public async Task Initialization_WithInMemoryConfiguration_InitializesDatabaseAndPublishesConfiguration()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var context = new PersistenceBaseTestContext();
            var configuration = context.PublishInMemoryConfiguration();

            await context.Persistence.Initialization.WaitAsync(cancellationToken);

            Assert.Same(configuration, context.PersistenceBase.Configuration);

            await using var dbContext = context.PersistenceBase.CreateDbContext();

            Assert.True(await dbContext.Database.CanConnectAsync(cancellationToken));
        }

        [Fact]
        public async Task Initialization_WithInMemoryConfiguration_CreatesDatabaseSchema()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var context = new PersistenceBaseTestContext();

            context.PublishInMemoryConfiguration();

            await context.Persistence.Initialization.WaitAsync(cancellationToken);

            await using var dbContext = context.PersistenceBase.CreateDbContext();

            Assert.True(await dbContext.Database.CanConnectAsync(cancellationToken));
            Assert.NotNull(dbContext.Users);
            Assert.NotNull(dbContext.RepositoryLocations);
            Assert.NotNull(dbContext.Studies);
            Assert.NotNull(dbContext.Series);
            Assert.NotNull(dbContext.Instances);
            Assert.NotNull(dbContext.Measurements);
            Assert.NotNull(dbContext.UnrealObjects);
        }

        [Fact]
        public async Task Initialization_WithEmptyConnectionString_DoesNotApplyConfiguration()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var context = new PersistenceBaseTestContext();

            context.ConfigurationSubject.OnNext(new PersistenceConfiguration
            {
                ConnectionString = string.Empty,
                Mode = PersistenceMode.InMemory
            });

            await context.Persistence.Initialization.WaitAsync(cancellationToken);

            Assert.Null(context.PersistenceBase.Configuration);
            Assert.Throws<InvalidOperationException>(() => context.PersistenceBase.CreateDbContext());
        }

        [Fact]
        public async Task Initialization_WithEquivalentSuccessfulConfiguration_KeepsOriginalConfiguration()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var context = new PersistenceBaseTestContext();
            var originalConfiguration = context.PublishInMemoryConfiguration();

            await context.Persistence.Initialization.WaitAsync(cancellationToken);

            var equivalentConfiguration = new PersistenceConfiguration
            {
                ConnectionString = originalConfiguration.ConnectionString,
                Mode = originalConfiguration.Mode
            };

            context.ConfigurationSubject.OnNext(equivalentConfiguration);

            await context.Persistence.Initialization.WaitAsync(cancellationToken);

            Assert.Same(originalConfiguration, context.PersistenceBase.Configuration);
            Assert.NotSame(equivalentConfiguration, context.PersistenceBase.Configuration);
        }

        [Fact]
        public async Task Initialization_AfterFailedConfiguration_CanRecoverWithValidConfiguration()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var context = new PersistenceBaseTestContext();
            var failureCount = 0;

            context.Persistence.ExceptionThrown += _ => failureCount++;

            context.ConfigurationSubject.OnNext(CreateInvalidSqlServerConfiguration());

            await Assert.ThrowsAnyAsync<Exception>(() => context.Persistence.Initialization.WaitAsync(cancellationToken));

            Assert.Null(context.PersistenceBase.Configuration);
            Assert.Equal(1, failureCount);

            var validConfiguration = context.PublishInMemoryConfiguration();

            await context.Persistence.Initialization.WaitAsync(cancellationToken);

            Assert.Same(validConfiguration, context.PersistenceBase.Configuration);
            Assert.Equal(1, failureCount);

            await using var dbContext = context.PersistenceBase.CreateDbContext();

            Assert.True(await dbContext.Database.CanConnectAsync(cancellationToken));
        }

        [Fact]
        public async Task Initialization_WhenSameConfigurationFailsTwice_RetriesInitialization()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var context = new PersistenceBaseTestContext();
            var configuration = CreateInvalidSqlServerConfiguration();
            var failureCount = 0;

            context.Persistence.ExceptionThrown += _ => failureCount++;

            context.ConfigurationSubject.OnNext(configuration);

            await Assert.ThrowsAnyAsync<Exception>(() => context.Persistence.Initialization.WaitAsync(cancellationToken));

            Assert.Null(context.PersistenceBase.Configuration);
            Assert.Equal(1, failureCount);

            context.ConfigurationSubject.OnNext(configuration);

            await Assert.ThrowsAnyAsync<Exception>(() => context.Persistence.Initialization.WaitAsync(cancellationToken));

            Assert.Null(context.PersistenceBase.Configuration);
            Assert.Equal(2, failureCount);
        }

        [Fact]
        public async Task TestConnectionAsync_WithInMemoryConfiguration_ConnectsWithoutApplyingConfiguration()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var context = new PersistenceBaseTestContext();

            var result = await context.Persistence.TestConnectionAsync(new PersistenceConfiguration
            {
                ConnectionString = $"PersistenceConnectionTest-{Guid.NewGuid():N}",
                Mode = PersistenceMode.InMemory
            }, cancellationToken);

            Assert.True(result.IsSuccessful);
            Assert.Null(result.Exception);
            Assert.Equal("Connection successful.", result.Message);
            Assert.Null(context.PersistenceBase.Configuration);
        }

        [Fact]
        public async Task TestConnectionAsync_WithEmptyConnectionString_ReturnsFailureWithoutApplyingConfiguration()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var context = new PersistenceBaseTestContext();

            var result = await context.Persistence.TestConnectionAsync(new PersistenceConfiguration
            {
                ConnectionString = string.Empty,
                Mode = PersistenceMode.InMemory
            }, cancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.Null(result.Exception);
            Assert.Contains("must not be empty", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Null(context.PersistenceBase.Configuration);
        }

        [Fact]
        public async Task TestConnectionAsync_WithInvalidSqlServerConnectionString_ReturnsFailureAndRaisesException()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var context = new PersistenceBaseTestContext();
            Exception? reportedException = null;

            context.Persistence.ExceptionThrown += exception => reportedException = exception;

            var result = await context.Persistence.TestConnectionAsync(CreateInvalidSqlServerConfiguration(), cancellationToken);

            Assert.False(result.IsSuccessful);
            Assert.NotNull(result.Exception);
            Assert.Same(result.Exception, reportedException);
            Assert.False(string.IsNullOrWhiteSpace(result.Message));
            Assert.Null(context.PersistenceBase.Configuration);
        }

        [Fact]
        public async Task TestConnectionAsync_WhenCanceledBeforeExecution_PropagatesCancellation()
        {
            using var context = new PersistenceBaseTestContext();
            using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

            cancellationSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Persistence.TestConnectionAsync(new PersistenceConfiguration
            {
                ConnectionString = $"CanceledConnectionTest-{Guid.NewGuid():N}",
                Mode = PersistenceMode.InMemory
            }, cancellationSource.Token));

            Assert.Null(context.PersistenceBase.Configuration);
        }

        [Fact]
        public async Task Initialization_WhenApplicationStops_IgnoresSubsequentConfiguration()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var context = new PersistenceBaseTestContext();

            context.ApplicationLifetime.Stop();

            context.ConfigurationSubject.OnNext(new PersistenceConfiguration
            {
                ConnectionString = $"StoppedPersistence-{Guid.NewGuid():N}",
                Mode = PersistenceMode.InMemory
            });

            await context.Persistence.Initialization.WaitAsync(cancellationToken);

            Assert.Null(context.PersistenceBase.Configuration);
            Assert.Throws<InvalidOperationException>(() => context.PersistenceBase.CreateDbContext());
        }

        private static PersistenceConfiguration CreateInvalidSqlServerConfiguration() => new()
        {
            ConnectionString = "InvalidConnectionStringKeyword=True;",
            Mode = PersistenceMode.SqlServer
        };

        private sealed class PersistenceBaseTestContext : IDisposable
        {
            public PersistenceBaseTestContext()
            {
                ConfigurationSubject = new BehaviorSubject<PersistenceConfiguration>(new PersistenceConfiguration());

                var persistence = new TestPersistenceBase(ApplicationLifetime, ConfigurationSubject);

                Persistence = persistence;
                PersistenceBase = persistence;
            }

            public TestApplicationLifetime ApplicationLifetime { get; } = new();

            public BehaviorSubject<PersistenceConfiguration> ConfigurationSubject { get; }

            public IPersistence Persistence { get; }

            public IPersistenceBase PersistenceBase { get; }

            public void Dispose()
            {
                ApplicationLifetime.Stop();
                ConfigurationSubject.Dispose();
                ApplicationLifetime.Dispose();
            }

            public PersistenceConfiguration PublishInMemoryConfiguration()
            {
                var configuration = new PersistenceConfiguration
                {
                    ConnectionString = $"PersistenceBaseTest-{Guid.NewGuid():N}",
                    Mode = PersistenceMode.InMemory
                };

                ConfigurationSubject.OnNext(configuration);

                return configuration;
            }
        }

        private sealed class TestApplicationLifetime : IApplicationLifetime, IDisposable
        {
            private readonly CancellationTokenSource _applicationStopping = new();

            public CancellationToken ApplicationStopping => _applicationStopping.Token;

            public void Dispose() => _applicationStopping.Dispose();

            public void Stop()
            {
                if (!_applicationStopping.IsCancellationRequested) _applicationStopping.Cancel();
            }
        }

        private sealed class TestPersistenceBase(IApplicationLifetime applicationLifetime, IObservable<PersistenceConfiguration> persistenceConfigurationObservable) : PersistenceBase(null, applicationLifetime, persistenceConfigurationObservable)
        {
        }
    }
}