using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Bases
{
    /// <summary>
    /// Provides the shared infrastructure for the Persistence module.
    /// </summary>
    internal abstract class PersistenceBase : IPersistenceBase, IPersistence
    {
        protected IDicomImportPersistenceService? _dicomImport;
        protected IInstanceRepository? _instance;
        protected IPersistenceIntegrityService? _integrity;
        protected IMeasurementRepository? _measurement;
        protected IRepositoryLocationRepository? _repositoryLocation;
        protected ISeriesRepository? _series;
        protected IStudyRepository? _study;
        protected IUnrealObjectRepository? _unrealObject;
        protected IUserRepository? _user;

        private readonly IApplicationLifetime _applicationLifetime;
        private readonly Lock _configurationSynchronization = new();
        private readonly Lock _exceptionThrownLock = new();
        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
        private readonly ILogger? _logger;
        private readonly IDisposable? _persistenceConfigurationSubscription;
        private readonly CancellationTokenRegistration _shutdownRegistration;

        private IDbContextFactory<PersistenceDbContext>? _dbContextFactory;
        private Action<Exception>? _exceptionThrown;
        private Task _initialization = Task.CompletedTask;
        private PersistenceConfiguration? _persistenceConfiguration;
        private ServiceProvider? _serviceProvider;

        internal PersistenceBase(
            ILogger? logger,
            IApplicationLifetime? applicationLifetime,
            IObservable<PersistenceConfiguration>? persistenceConfigurationObservable)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime
                ?? throw new ArgumentNullException(
                    nameof(applicationLifetime),
                    "Application lifetime cannot be null.");

            // Shutdown prevents further configuration notifications and releases
            // the provider after in-flight operations receive the stopping token.
            _shutdownRegistration = _applicationLifetime.ApplicationStopping
                .Register(DisposePersistenceInfrastructure);

            // BehaviorSubject delivers its current value synchronously. Storing the
            // resulting task allows consumers to await the matching initialization.
            _persistenceConfigurationSubscription =
                persistenceConfigurationObservable?
                    .Subscribe(QueueConfigurationInitialization);
        }

        /// <inheritdoc/>
        public event Action<Exception> ExceptionThrown
        {
            add
            {
                lock (_exceptionThrownLock)
                {
                    _exceptionThrown += value;
                }
            }
            remove
            {
                lock (_exceptionThrownLock)
                {
                    _exceptionThrown -= value;
                }
            }
        }

        /// <inheritdoc/>
        IApplicationLifetime? IPersistenceBase.ApplicationLifetime =>
            _applicationLifetime;

        /// <inheritdoc/>
        PersistenceConfiguration? IPersistenceBase.Configuration =>
            _persistenceConfiguration;

        /// <inheritdoc/>
        public IDicomImportPersistenceService? DicomImport => _dicomImport;

        /// <inheritdoc/>
        public Task Initialization
        {
            get
            {
                lock (_configurationSynchronization)
                {
                    return _initialization;
                }
            }
        }

        /// <inheritdoc/>
        public IInstanceRepository? Instance => _instance;

        /// <inheritdoc/>
        public IPersistenceIntegrityService? Integrity => _integrity;

        /// <inheritdoc/>
        ILogger? IPersistenceBase.Logger => _logger;

        /// <inheritdoc/>
        public IMeasurementRepository? Measurement => _measurement;

        /// <inheritdoc/>
        public IRepositoryLocationRepository? RepositoryLocation =>
            _repositoryLocation;

        /// <inheritdoc/>
        public ISeriesRepository? Series => _series;

        /// <inheritdoc/>
        public IStudyRepository? Study => _study;

        /// <inheritdoc/>
        public IUnrealObjectRepository? UnrealObject => _unrealObject;

        /// <inheritdoc/>
        public IUserRepository? User => _user;

        /// <inheritdoc/>
        PersistenceDbContext IPersistenceBase.CreateDbContext()
        {
            var dbContextFactory = _dbContextFactory
                ?? throw new InvalidOperationException(
                    "Persistence has not been configured.");

            return dbContextFactory.CreateDbContext();
        }

        /// <inheritdoc/>
        async Task IPersistenceBase.InitializeDatabaseAsync(
            CancellationToken cancellationToken)
        {
            await _initializationSemaphore
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                var dbContextFactory = _dbContextFactory
                    ?? throw new InvalidOperationException(
                        "Persistence has not been configured.");

                await using var context =
                    dbContextFactory.CreateDbContext();

                if (!context.Database.IsRelational())
                {
                    // Relational migration APIs are unavailable for the EF Core
                    // in-memory provider used by integration tests.
                    await context.Database
                        .EnsureCreatedAsync(cancellationToken)
                        .ConfigureAwait(false);

                    return;
                }

                var pendingMigrations = await context.Database
                    .GetPendingMigrationsAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (pendingMigrations.Any())
                {
                    await context.Database
                        .MigrateAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        /// <inheritdoc/>
        void IPersistenceBase.OnExceptionThrown(Exception exception)
        {
            _logger?.LogError(
                exception,
                "Exception thrown in {AssemblyName}",
                Assembly.GetCallingAssembly().GetName().Name);

            Action<Exception>? handlers;

            lock (_exceptionThrownLock)
            {
                handlers = _exceptionThrown;
            }

            if (handlers is null)
            {
                return;
            }

            // Subscriber exceptions are isolated so every registered diagnostics
            // consumer receives the original Persistence exception.
            foreach (var handler in handlers
                .GetInvocationList()
                .Cast<Action<Exception>>())
            {
                try
                {
                    handler(exception);
                }
                catch (Exception callbackException)
                {
                    _logger?.LogError(
                        callbackException,
                        "Exception thrown by ExceptionThrown event handler.");
                }
            }
        }

        /// <inheritdoc/>
        async Task<PersistenceConnectionTestResult>
            IPersistenceBase.TestConnectionAsync(
                CancellationToken cancellationToken)
        {
            try
            {
                var dbContextFactory = _dbContextFactory
                    ?? throw new InvalidOperationException(
                        "Persistence has not been configured.");

                await using var context =
                    dbContextFactory.CreateDbContext();

                var isSuccessful = await context.Database
                    .CanConnectAsync(cancellationToken)
                    .ConfigureAwait(false);

                return CreateConnectionTestResult(isSuccessful);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                ((IPersistenceBase)this).OnExceptionThrown(exception);
                return CreateConnectionTestFailure(exception);
            }
        }

        /// <inheritdoc/>
        public async Task<PersistenceConnectionTestResult>
            TestConnectionAsync(
                PersistenceConfiguration configuration,
                CancellationToken cancellationToken = default)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(
                configuration.ConnectionString))
            {
                return new PersistenceConnectionTestResult
                {
                    IsSuccessful = false,
                    Message = "The connection string must not be empty."
                };
            }

            try
            {
                var services = new ServiceCollection();

                services.AddDbContextFactory<PersistenceDbContext>(
                    options => ConfigureDatabase(
                        options,
                        configuration));

                using var serviceProvider =
                    services.BuildServiceProvider();

                var dbContextFactory = serviceProvider
                    .GetRequiredService<
                        IDbContextFactory<PersistenceDbContext>>();

                await using var context =
                    dbContextFactory.CreateDbContext();

                if (!context.Database.IsRelational())
                {
                    await context.Database
                        .EnsureCreatedAsync(cancellationToken)
                        .ConfigureAwait(false);
                }

                var isSuccessful = await context.Database
                    .CanConnectAsync(cancellationToken)
                    .ConfigureAwait(false);

                return CreateConnectionTestResult(isSuccessful);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                ((IPersistenceBase)this).OnExceptionThrown(exception);
                return CreateConnectionTestFailure(exception);
            }
        }

        private async Task ApplyConfigurationAfterAsync(
            Task previousInitialization,
            PersistenceConfiguration configuration,
            CancellationToken cancellationToken)
        {
            try
            {
                // Configuration changes are applied in publication order. Waiting
                // also prevents disposal while the earlier provider is still used.
                try
                {
                    await previousInitialization.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch
                {
                    // A failed earlier configuration must not permanently prevent
                    // a later valid configuration from restoring Persistence.
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(
                        configuration.ConnectionString)
                    || IsCurrentConfiguration(configuration))
                {
                    return;
                }

                RebuildDbContextFactory(configuration);
                _persistenceConfiguration = configuration;

                await ((IPersistenceBase)this)
                    .InitializeDatabaseAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                ((IPersistenceBase)this).OnExceptionThrown(exception);
                throw;
            }
        }

        private static void ConfigureDatabase(
            DbContextOptionsBuilder options,
            PersistenceConfiguration configuration)
        {
            switch (configuration.Mode)
            {
                case PersistenceMode.InMemory:
                    // The connection string is the logical database name for
                    // isolated test databases using the in-memory provider.
                    options.UseInMemoryDatabase(
                        configuration.ConnectionString);
                    break;

                case PersistenceMode.SqlServer:
                default:
                    options.UseSqlServer(
                        configuration.ConnectionString);
                    break;
            }
        }

        private static PersistenceConnectionTestResult
            CreateConnectionTestFailure(Exception exception) =>
            new()
            {
                IsSuccessful = false,
                Message = exception.Message,
                Exception = exception
            };

        private static PersistenceConnectionTestResult
            CreateConnectionTestResult(bool isSuccessful) =>
            new()
            {
                IsSuccessful = isSuccessful,
                Message = isSuccessful
                    ? "Connection successful."
                    : "Connection failed."
            };

        private void DisposePersistenceInfrastructure()
        {
            try
            {
                _persistenceConfigurationSubscription?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Concurrent shutdown may already have disposed the subscription.
            }

            try
            {
                _serviceProvider?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Provider disposal is idempotent from the application perspective.
            }
        }

        private bool IsCurrentConfiguration(
            PersistenceConfiguration configuration)
        {
            var currentConfiguration = _persistenceConfiguration;

            return currentConfiguration is not null
                && currentConfiguration.Mode == configuration.Mode
                && string.Equals(
                    currentConfiguration.ConnectionString,
                    configuration.ConnectionString,
                    StringComparison.Ordinal);
        }

        private void QueueConfigurationInitialization(
            PersistenceConfiguration configuration)
        {
            lock (_configurationSynchronization)
            {
                _initialization = ApplyConfigurationAfterAsync(
                    _initialization,
                    configuration,
                    _applicationLifetime.ApplicationStopping);
            }
        }

        private void RebuildDbContextFactory(
            PersistenceConfiguration configuration)
        {
            var services = new ServiceCollection();

            services.AddDbContextFactory<PersistenceDbContext>(
                options => ConfigureDatabase(
                    options,
                    configuration));

            var previousServiceProvider = _serviceProvider;
            var newServiceProvider = services.BuildServiceProvider();

            try
            {
                var newDbContextFactory = newServiceProvider
                    .GetRequiredService<
                        IDbContextFactory<PersistenceDbContext>>();

                _serviceProvider = newServiceProvider;
                _dbContextFactory = newDbContextFactory;
            }
            catch
            {
                newServiceProvider.Dispose();
                throw;
            }

            previousServiceProvider?.Dispose();
        }
    }
}