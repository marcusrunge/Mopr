using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Miras;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts
{
    /// <summary>
    /// Provides MIRAS application services through the Core module.
    /// </summary>
    public interface IMirasApplicationService
    {
        /// <summary>
        /// Gets the MIRAS execution flow.
        /// </summary>
        IMirasFlowService MirasFlowService { get; }
    }
}