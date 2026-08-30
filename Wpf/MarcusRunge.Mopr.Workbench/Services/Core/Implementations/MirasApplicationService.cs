using MarcusRunge.Mopr.Workbench.Services.Core.Bases;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Implementations
{
    /// <summary>
    /// Composes the MIRAS services owned by one Core module instance.
    /// </summary>
    internal sealed class MirasApplicationService : MirasApplicationServiceBase
    {
        internal MirasApplicationService(ICoreBase? coreBase) : base(coreBase) => _mirasFlowService = Miras.MirasFlowService.Create(this);

        internal static IMirasApplicationService? Create(ICoreBase? coreBase) => coreBase is null ? null : new MirasApplicationService(coreBase);
    }
}