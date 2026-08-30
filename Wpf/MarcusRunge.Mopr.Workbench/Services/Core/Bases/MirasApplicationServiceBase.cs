using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Miras;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Bases
{
    /// <summary>
    /// Provides the shared service references and Core context for the
    /// MIRAS service.
    /// </summary>
    internal abstract class MirasApplicationServiceBase(ICoreBase? coreBase) : IMirasApplicationServiceBase, IMirasApplicationService
    {
        protected IMirasFlowService? _mirasFlowService;

        /// <inheritdoc/>
        ICoreBase IMirasApplicationServiceBase.CoreBase => CoreBase;

        /// <inheritdoc/>
        public IMirasFlowService MirasFlowService => _mirasFlowService ?? throw new InvalidOperationException("The MIRAS flow service has not been initialized.");

        /// <summary>
        /// Gets the owning Core module context.
        /// </summary>
        protected ICoreBase CoreBase { get; } = coreBase ?? throw new ArgumentNullException(nameof(coreBase));
    }
}