using MarcusRunge.Mopr.Workbench.Services.Core.Bases;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Implementations
{
    // Concrete internal module implementation that wires up services for this module instance.
    internal sealed class Core : CoreBase
    {
        internal Core(ILogger? logger) : base(logger)
        {
            // What happens here:
            // - The assembly constructor performs "composition" for this module instance by creating and assigning
            //   the concrete service implementations to the protected backing fields defined in the base class.

            // Service creation pattern:
            // - Each service is created via its static Create(...) factory.
            // - The current assembly instance ('this') is passed as the base/context argument so the service can:
            //   - access assembly-provided dependencies,
            //   - register itself with module state,
            //   - or use the assembly as an initialization context.

            // Ordering / intention:
            // - Services are created in a defined order.
            // - This can be important if later services assume earlier services exist or if initialization
            //   side-effects are expected in that sequence.

            // Resulting state:
            // - After the constructor finishes, the assembly's accessors
            //   (exposed by the base class / interfaces) return these created instances.
            // - The assembly is therefore "ready for use" regarding these service references.
            _imagingService = Implementations.ImagingService.Create(this);
        }
    }
}