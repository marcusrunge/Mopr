using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Application
{
    /// <summary>
    /// Defines a factory contract for creating a clean architecture module instance.
    /// </summary>
    public interface IApplicationFactory
    {
        /// <summary>
        /// Creates (or returns) a module instance.
        /// </summary>
        IApplication Create();
    }

    /// <summary>
    /// Default factory implementation that provides a factory and module instance.
    /// </summary>
    public class ApplicationFactory : IApplicationFactory
    {
        // Stores the module instance created by this factory (lazy-created).
        private IApplication? _moduleInstance;

        // Logger reference for potential logging; can be null if not provided.
        private readonly ILogger? _logger;

        public ApplicationFactory()
        {
        }

        public ApplicationFactory(ILogger? logger)
        {
            _logger = logger;
        }


        /// <inheritdoc/>
        public IApplication Create() =>
            /* What happens here:
               - Lazy initialization of the instance.
               - If _moduleInstance is null, a new Implementations.MarcusRunge.Mopr.Workbench.Services.Wpf is created and cached.
               - If it is already set, the cached module instance is returned.

               Purpose/intent:
               - Ensures consumers get a single shared module instance per process/app-domain-like context,
                 created on first demand. */
            _moduleInstance ??= new Implementations.Application(_logger);
    }
}