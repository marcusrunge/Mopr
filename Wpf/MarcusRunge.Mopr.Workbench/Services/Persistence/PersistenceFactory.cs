using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence
{
    /// <summary>
    /// Defines a factory contract for creating a clean architecture module instance.
    /// </summary>
    public interface IPersistenceFactory
    {
        /// <summary>
        /// Creates (or returns) a module instance.
        /// </summary>
        IPersistence Create();
    }

    /// <summary>
    /// Default factory implementation that provides a singleton-like factory and module instance.
    /// </summary>
    public class PersistenceFactory : IPersistenceFactory
    {
        // Reference to the application lifetime, used for managing application shutdown and cancellation.
        private readonly IApplicationLifetime? _applicationLifetime;
        // Stores the  module instance created by this factory (lazy-created).
        private IPersistence? _moduleInstance;

        // Logger reference for potential logging; can be null if not provided.
        private readonly ILogger? _logger;

        // Observable for the persistence configuration.
        private readonly IObservable<PersistenceConfiguration> _persistenceConfigurationObservable;

        public PersistenceFactory(IApplicationLifetime applicationLifetime, IObservable<PersistenceConfiguration> persistenceConfigurationObservable)
        {
            _applicationLifetime = applicationLifetime;
            _persistenceConfigurationObservable = persistenceConfigurationObservable;
        }

        public PersistenceFactory(ILogger? logger, IApplicationLifetime applicationLifetime, IObservable<PersistenceConfiguration> persistenceConfigurationObservable)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _persistenceConfigurationObservable = persistenceConfigurationObservable;
        }


        /// <inheritdoc/>
        public IPersistence Create() =>
            /* What happens here:
               - Lazy initialization of the instance.
               - If _moduleInstance is null, a new Implementations.MarcusRunge.Mopr.Workbench.Services.Persistence is created and cached.
               - If it is already set, the cached module instance is returned.

               Purpose/intent:
               - Ensures consumers get a single shared module instance per process/app-domain-like context,
                 created on first demand. */
            _moduleInstance ??= new Implementations.Persistence(_logger, _applicationLifetime, _persistenceConfigurationObservable);
    }
}