using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Bases
{
    internal abstract class IdentityServiceBase(IApplicationBase? applicationBase) : IIdentityServiceBase, IIdentityService
    {
        protected IOperatingSystemIdentityProvider? _operatingSystemIdentityProvider;

        IApplicationBase? IServiceBase.ApplicationBase => applicationBase;
        public IOperatingSystemIdentityProvider? OperatingSystemIdentityProvider => _operatingSystemIdentityProvider;
    }
}