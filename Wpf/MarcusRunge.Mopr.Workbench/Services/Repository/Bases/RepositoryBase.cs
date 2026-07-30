using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Bases
{
    // Internal base for modules; holds optional service references for derived types.
    internal abstract class RepositoryBase : IRepositoryBase, IRepository
    {
        protected IDicomImportService? _importService;
        protected IDicomRepositoryRepairService? _repositoryRepairService;
        protected IDicomRepositoryService? _repositoryService;

        // Subscription to the persistence configuration observable; used to receive updates and initialize the database accordingly.
        private readonly IDisposable? _applicationConfigurationSubscription;

        // Reference to the application lifetime, used for managing application shutdown and cancellation.
        private readonly IApplicationLifetime? _applicationLifetime;

        // Lock object to synchronize access to the ExceptionThrown event handlers.
        private readonly Lock _exceptionThrownLock = new();

        private readonly ILogger? _logger;
        private readonly IPersistence? _persistence;

        // Registration for application shutdown; used to dispose of the persistence configuration subscription when the application is stopping.
        private readonly CancellationTokenRegistration _shutdownRegistration;

        private IApplicationConfiguration? _applicationConfiguration;

        // Backing field for the ExceptionThrown event handlers.
        private Action<Exception>? _exceptionThrown;

        internal RepositoryBase(ILogger? logger, IApplicationLifetime? applicationLifetime, IObservable<IApplicationConfiguration>? applicationConfigurationObservable, IPersistence? persistence)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _persistence = persistence;

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
                    _applicationConfigurationSubscription?.Dispose();
                }
                catch { }
            });

            // Subscribe to the persistence configuration observable to receive updates and initialize the database accordingly.
            _applicationConfigurationSubscription = applicationConfigurationObservable?.Subscribe(configuration =>
            {
                // Handle the configuration change asynchronously, allowing for dynamic updates to the database connection string and reinitialization of the database.
                _ = HandleConfigurationChangedAsync(configuration);
            });
        }

        /// <inheritdoc/>
        public event Action<Exception> ExceptionThrown
        {
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
        IApplicationConfiguration? IRepositoryBase.ApplicationConfiguration => _applicationConfiguration;

        /// <inheritdoc/>
        public IDicomImportService? ImportService => _importService;

        /// <inheritdoc/>
        ILogger? IRepositoryBase.Logger => _logger;

        /// <inheritdoc/>
        IPersistence? IRepositoryBase.Persistence => _persistence;

        /// <inheritdoc/>
        public IDicomRepositoryRepairService? RepositoryRepairService => _repositoryRepairService;

        /// <inheritdoc/>
        public IDicomRepositoryService? RepositoryService => _repositoryService;

        /// <inheritdoc/>
        void IRepositoryBase.OnExceptionThrown(Exception exception)
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

        // Handles changes to the persistence configuration by rebuilding the DbContextFactory and initializing the database.
        private async Task HandleConfigurationChangedAsync(IApplicationConfiguration configuration)
        {
            // Use a try-catch block to handle any exceptions that may occur during the configuration change handling process.
            try
            {
                // Update the internal configuration reference with the new configuration.
                _applicationConfiguration = configuration;
            }
            catch (Exception exception)
            {
                ((IRepositoryBase)this).OnExceptionThrown(exception);
            }
        }
    }
}