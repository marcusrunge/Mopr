using MarcusRunge.Mopr.Workbench.Contracts.Application;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Bases
{
    // Internal base for modules; holds optional service references for derived types.
    internal abstract class PersistenceBase : IPersistenceBase, IPersistence
    {
        // Backing field for IInstanceRepository (assigned by derived modules)
        protected IInstanceRepository? _instance;

        // Backing field for IPersistenceIntegrityService (assigned by derived modules)
        protected IPersistenceIntegrityService? _integrity;

        // Backing field for IMeasurementRepository (assigned by derived modules)
        protected IMeasurementRepository? _measurement;

        // Backing field for IRepositoryLocationRepository (assigned by derived modules)
        protected IRepositoryLocationRepository? _repositoryLocation;

        // Backing field for ISeriesRepository (assigned by derived modules)
        protected ISeriesRepository? _series;

        // Backing field for IStudyRepository (assigned by derived modules)
        protected IStudyRepository? _study;

        // Backing field for IUnrealObjectRepository (assigned by derived modules)
        protected IUnrealObjectRepository? _unrealObject;

        // Backing field for IUserRepository (assigned by derived modules)
        protected IUserRepository? _user;

        // Reference to the application lifetime, used for managing application shutdown and cancellation.
        private readonly IApplicationLifetime? _applicationLifetime;

        // Lock object to synchronize access to the ExceptionThrown event handlers.
        private readonly Lock _exceptionThrownLock = new();

        // Semaphore to ensure that database initialization is performed only once, even in multi-threaded scenarios.
        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);

        // Logger instance for logging within the module.
        private readonly ILogger? _logger;

        // Subscription to the persistence configuration observable; used to receive updates and initialize the database accordingly.
        private readonly IDisposable? _persistenceConfigurationSubscription;

        // Registration for application shutdown; used to dispose of the persistence configuration subscription when the application is stopping.
        private readonly CancellationTokenRegistration _shutdownRegistration;

        // Factory for creating instances of the PersistenceDbContext; used for database operations.
        private IDbContextFactory<PersistenceDbContext>? _dbContextFactory;

        // Backing field for the ExceptionThrown event handlers.
        private Action<Exception>? _exceptionThrown;

        // Backing field for the configuration; set by constructor subscription.
        private PersistenceConfiguration? _persistenceConfiguration;

        private ServiceProvider? _serviceProvider;

        internal PersistenceBase(ILogger? logger, IApplicationLifetime? applicationLifetime, IObservable<PersistenceConfiguration>? persistenceConfigurationObservable)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            // Ensure that the application lifetime is not null; throw an exception if it is.
            if (applicationLifetime is null)
            {
                throw new ArgumentNullException(nameof(applicationLifetime), "Application lifetime cannot be null.");
            }
            // Register a callback to dispose of the persistence configuration subscription when the application is stopping.
            _shutdownRegistration = applicationLifetime.ApplicationStopping.Register(() =>
            {
                try
                {
                    // Dispose of the persistence configuration subscription to clean up resources and prevent memory leaks.
                    _persistenceConfigurationSubscription?.Dispose();
                }
                catch { }
                try
                {
                    // Dispose of the service provider to clean up resources and prevent memory leaks.
                    _serviceProvider?.Dispose();
                }
                catch { }
            });

            // Subscribe to the persistence configuration observable to receive updates and initialize the database accordingly.
            _persistenceConfigurationSubscription = persistenceConfigurationObservable?.Subscribe(configuration =>
            {
                // Handle the configuration change asynchronously, allowing for dynamic updates to the database connection string and reinitialization of the database.
                _ = HandleConfigurationChangedAsync(configuration);
            });
        }

        /// <inheritdoc/>
        public event Action<Exception> ExceptionThrown
        {
            // Use a lock to ensure thread-safe addition and removal of event handlers.
            add
            {
                lock (_exceptionThrownLock) _exceptionThrown += value;
            }
            remove
            {
                lock (_exceptionThrownLock) _exceptionThrown -= value;
            }
        }

        /// <inheritdoc/>
        IApplicationLifetime? IPersistenceBase.ApplicationLifetime => _applicationLifetime;

        /// <inheritdoc/>
        PersistenceConfiguration? IPersistenceBase.Configuration => _persistenceConfiguration;

        /// <inheritdoc/>
        public IInstanceRepository? Instance => _instance;

        /// <inheritdoc/>
        public IPersistenceIntegrityService? Integrity => _integrity;

        /// <inheritdoc/>
        ILogger? IPersistenceBase.Logger => _logger;

        /// <inheritdoc/>
        public IMeasurementRepository? Measurement => _measurement;

        /// <inheritdoc/>
        public IRepositoryLocationRepository? RepositoryLocation => _repositoryLocation;

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
            // Ensure that the DbContextFactory has been initialized before attempting to create a new DbContext instance.
            if (_dbContextFactory == null)
            {
                // If the DbContextFactory is null, throw an InvalidOperationException to indicate that persistence has not been configured.
                throw new InvalidOperationException("Persistence has not been configured.");
            }
            // Use the DbContextFactory to create and return a new instance of the PersistenceDbContext.
            return _dbContextFactory.CreateDbContext();
        }

        /// <inheritdoc/>
        async Task IPersistenceBase.InitializeDatabaseAsync(CancellationToken cancellationToken)
        {
            // Ensure that only one thread can perform the database initialization at a time.
            await _initializationSemaphore.WaitAsync(cancellationToken);
            // Use a try-finally block to ensure that the semaphore is released even if an exception occurs during initialization.
            try
            {
                // Ensure that the DbContextFactory has been initialized before attempting to create a new DbContext instance.
                await using var context = (_dbContextFactory?.CreateDbContext()) ?? throw new InvalidOperationException("Failed to create database context.");
                // Check for any pending migrations that need to be applied to the database.
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
                // If there are any pending migrations, apply them to the database to ensure that it is up-to-date with the current schema.
                if (pendingMigrations.Any())
                {
                    await context.Database.MigrateAsync(cancellationToken);
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
            // Log the exception with the module's logger, if available.
            _logger?.LogError(exception, "Exception thrown in {AssemblyName}", Assembly.GetCallingAssembly().GetName().Name);
            // Capture the current handlers to invoke outside the lock.
            Action<Exception>? handlers;
            // Lock to safely read the current handlers.
            lock (_exceptionThrownLock)
            {
                // Capture the current handlers to invoke outside the lock.
                handlers = _exceptionThrown;
            }
            // If there are no handlers, there's nothing to invoke.
            if (handlers is null)
                return;
            // Invoke each handler in a try-catch to ensure one failing handler doesn't prevent others from being notified.
            foreach (Action<Exception> handler in handlers.GetInvocationList().Cast<Action<Exception>>())
            {
                try
                {
                    // Invoke the handler with the exception.
                    handler(exception);
                }
                catch (Exception callbackException)
                {
                    // Log any exceptions thrown by the handlers, but continue invoking the remaining handlers.
                    _logger?.LogError(callbackException, "Exception thrown by ExceptionThrown event handler.");
                }
            }
        }

        /// <inheritdoc/>
        async Task<PersistenceConnectionTestResult> IPersistenceBase.TestConnectionAsync(CancellationToken cancellationToken)
        {
            // Attempt to create a new DbContext and test the database connection.
            try
            {
                // Ensure that the DbContextFactory has been initialized before attempting to create a new DbContext instance.
                if (_dbContextFactory == null)
                {
                    // If the DbContextFactory is null, throw an InvalidOperationException to indicate that persistence has not been configured.
                    throw new InvalidOperationException("Persistence has not been configured.");
                }
                // Create a new DbContext instance using the provided factory.
                await using var context = _dbContextFactory.CreateDbContext();
                // Test if the database connection can be established.
                var result = await context.Database.CanConnectAsync(cancellationToken);
                // Return the result of the connection test, indicating success or failure.
                return new PersistenceConnectionTestResult
                {
                    IsSuccessful = result,
                    Message = result ? "Connection successful." : "Connection failed."
                };
            }
            catch (Exception exception)
            {
                // If an exception occurs during the connection test, log it and notify any registered handlers.
                ((IPersistenceBase)this).OnExceptionThrown(exception);
                // Return a result indicating the failure and include the exception details.
                return new PersistenceConnectionTestResult
                {
                    IsSuccessful = false,
                    Message = exception.Message,
                    Exception = exception
                };
            }
        }

        // Handles changes to the persistence configuration by rebuilding the DbContextFactory and initializing the database.
        private async Task HandleConfigurationChangedAsync(PersistenceConfiguration configuration)
        {
            // If the new configuration's connection string is null, empty, or whitespace, or if it matches the existing configuration's connection string, no action is needed.
            if (string.IsNullOrWhiteSpace(configuration.ConnectionString) || _persistenceConfiguration?.ConnectionString == configuration.ConnectionString)
            {
                return;
            }

            // Use a try-catch block to handle any exceptions that may occur during the configuration change handling process.
            try
            {
                // Update the internal configuration reference with the new configuration.
                _persistenceConfiguration = configuration;

                // Rebuild the DbContextFactory with the new configuration, allowing for dynamic updates to the database connection string.
                RebuildDbContextFactory(configuration);
                // Initialize the database asynchronously, ensuring that any pending migrations are applied and the database is ready for use.
                await ((IPersistenceBase)this).InitializeDatabaseAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                ((IPersistenceBase)this).OnExceptionThrown(exception);
            }
        }

        // Rebuilds the DbContextFactory using the current persistence configuration.
        private void RebuildDbContextFactory(PersistenceConfiguration configuration)
        {
            // Create a new service collection for rebuilding the DbContextFactory.
            var services = new ServiceCollection();
            // Add the PersistenceDbContext to the service collection using the configured database provider.
            services.AddDbContextFactory<PersistenceDbContext>(
                options =>
                {
                    switch (configuration.Mode)
                    {
                        case PersistenceMode.InMemory:
                            /*
                             * The configured connection string acts as the logical database name.
                             * Each fixture provides a unique value so independent test modules cannot
                             * accidentally share the same EF Core In-Memory database.
                             */
                            options.UseInMemoryDatabase(configuration.ConnectionString);
                            break;

                        case PersistenceMode.SqlServer:
                        default:
                            options.UseSqlServer(configuration.ConnectionString);
                            break;
                    }
                });
            // Dispose of the existing service provider, if any, to clean up resources and prevent memory leaks.
            _serviceProvider?.Dispose();
            // Build the service provider from the configured services.
            _serviceProvider = services.BuildServiceProvider();
            // Retrieve the newly configured DbContextFactory.
            _dbContextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<PersistenceDbContext>>();
        }
    }
}