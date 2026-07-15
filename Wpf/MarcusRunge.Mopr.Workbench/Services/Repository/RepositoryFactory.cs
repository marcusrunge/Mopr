using MarcusRunge.Mopr.Workbench.Contracts.Application;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Repository
{
    /// <summary>
    /// Defines a factory contract for creating a clean architecture module instance.
    /// </summary>
    public interface IRepositoryFactory
    {
        /// <summary>
        /// Creates (or returns) a module instance.
        /// </summary>
        IRepository Create();
    }

    /// <summary>
    /// Default factory implementation that provides a singleton-like factory and module instance.
    /// </summary>
    public class RepositoryFactory : IRepositoryFactory
    {
        // Stores the singleton-like module instance created by this factory (lazy-created).
        private static IRepository? _moduleInstance;

        private readonly IObservable<IApplicationConfiguration>? _applicationConfigurationObservable;

        // Reference to the application lifetime, used for managing application shutdown and cancellation.
        private readonly IApplicationLifetime? _applicationLifetime;

        // Logger reference for potential logging; can be null if not provided.
        private readonly ILogger? _logger;

        public RepositoryFactory(IApplicationLifetime? applicationLifetime, IObservable<IApplicationConfiguration>? applicationConfigurationObservable)
        {
            _applicationLifetime = applicationLifetime;
            _applicationConfigurationObservable = applicationConfigurationObservable;
        }

        public RepositoryFactory(ILogger? logger, IApplicationLifetime? applicationLifetime, IObservable<IApplicationConfiguration>? applicationConfigurationObservable)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _applicationConfigurationObservable = applicationConfigurationObservable;
        }

        /// <inheritdoc/>
        public IRepository Create() =>
            /* What happens here:
               - Lazy initialization of the instance.
               - If _moduleInstance is null, a new Implementations.MarcusRunge.Mopr.Workbench.Services.Repository is created and cached.
               - If it is already set, the cached module instance is returned.

               Purpose/intent:
               - Ensures consumers get a single shared module instance per process/app-domain-like context,
                 created on first demand. */
            _moduleInstance ??= new Implementations.Repository(_logger, _applicationLifetime, _applicationConfigurationObservable);
    }
}