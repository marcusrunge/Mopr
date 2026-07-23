using MarcusRunge.Mopr.Workbench.Contracts.Application;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Miras
{
    /// <summary>
    /// Defines a factory contract for creating a clean architecture module instance.
    /// </summary>
    public interface IMirasFactory
    {
        /// <summary>
        /// Creates (or returns) a module instance.
        /// </summary>
        IMiras Create();
    }

    /// <summary>
    /// Default factory implementation that provides a singleton-like factory and module instance.
    /// </summary>
    public class MirasFactory : IMirasFactory
    {
        // Stores the singleton-like module instance created by this factory (lazy-created).
        private static IMiras? _moduleInstance;

        // Logger reference for potential logging; can be null if not provided.
        private readonly ILogger? _logger;
        // Reference to the application lifetime, used for managing application shutdown and cancellation.
        private readonly IApplicationLifetime? _applicationLifetime;
        // Reference to persistence
        private readonly IPersistence? _persistence;
        // Reference to repostitory
        private readonly IRepository? _repository;
        public MirasFactory(IApplicationLifetime? applicationLifetime, IPersistence persistence, IRepository repository)
        {
            _applicationLifetime = applicationLifetime;
            _persistence = persistence;
            _repository = repository;
        }

        public MirasFactory(ILogger? logger, IApplicationLifetime? applicationLifetime, IPersistence persistence, IRepository repository)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _persistence = persistence;
            _repository = repository;
        }


        /// <inheritdoc/>
        public IMiras Create() =>
            /* What happens here:
               - Lazy initialization of the instance.
               - If _moduleInstance is null, a new Implementations.MarcusRunge.Mopr.Workbench.Services.Miras is created and cached.
               - If it is already set, the cached module instance is returned.

               Purpose/intent:
               - Ensures consumers get a single shared module instance per process/app-domain-like context,
                 created on first demand. */
            _moduleInstance ??= new Implementations.Miras(_logger, _applicationLifetime, _persistence,_repository);
    }
}