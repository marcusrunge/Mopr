using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Wpf
{
    /// <summary>
    /// Defines a factory contract for creating a clean architecture module instance.
    /// </summary>
    public interface IWpfFactory
    {
        /// <summary>
        /// Creates (or returns) a module instance.
        /// </summary>
        IWpf Create();
    }

    /// <summary>
    /// Default factory implementation that provides a singleton-like factory and module instance.
    /// </summary>
    public class WpfFactory : IWpfFactory
    {
        // Stores the singleton-like module instance created by this factory (lazy-created).
        private static IWpf? _moduleInstance;

        // Logger reference for potential logging; can be null if not provided.
        private readonly ILogger? _logger;

        public WpfFactory()
        {
        }

        public WpfFactory(ILogger? logger)
        {
            _logger = logger;
        }


        /// <inheritdoc/>
        public IWpf Create() =>
            /* What happens here:
               - Lazy initialization of the instance.
               - If _moduleInstance is null, a new Implementations.MarcusRunge.Mopr.Workbench.Services.Wpf is created and cached.
               - If it is already set, the cached module instance is returned.

               Purpose/intent:
               - Ensures consumers get a single shared module instance per process/app-domain-like context,
                 created on first demand. */
            _moduleInstance ??= new Implementations.Wpf(_logger);
    }
}