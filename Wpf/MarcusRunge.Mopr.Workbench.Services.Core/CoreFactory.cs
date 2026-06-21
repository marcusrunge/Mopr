using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services
{
    /// <summary>
    /// Defines a factory contract for creating a clean architecture module instance.
    /// </summary>
    public interface ICoreFactory
    {
        /// <summary>
        /// Creates (or returns) a module instance.
        /// </summary>
        ICore Create();
    }

    /// <summary>
    /// Default factory implementation that provides a singleton-like factory and module instance.
    /// </summary>
    public class CoreFactory : ICoreFactory
    {
        // Stores the singleton-like module instance created by this factory (lazy-created).
        private static ICore? _moduleInstance;

        // Logger reference for potential logging; can be null if not provided.
        private readonly ILogger? _logger;

        public CoreFactory()
        {
        }

        public CoreFactory(ILogger? logger)
        {
            _logger = logger;
        }


        /// <inheritdoc/>
        public ICore Create() =>
            /* What happens here:
               - Lazy initialization of the instance.
               - If _moduleInstance is null, a new Implementations.MarcusRunge.Mopr.Workbench.Services.Core is created and cached.
               - If it is already set, the cached module instance is returned.

               Purpose/intent:
               - Ensures consumers get a single shared module instance per process/app-domain-like context,
                 created on first demand. */
            _moduleInstance ??= new Core.Implementations.Core(_logger);
    }
}