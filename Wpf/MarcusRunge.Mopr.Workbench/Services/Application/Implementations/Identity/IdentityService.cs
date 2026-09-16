using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Bases;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Identity
{
    internal class IdentityService : IdentityServiceBase
    {
        internal IdentityService(IApplicationBase? applicationBase) : base(applicationBase)
        {
            _operatingSystemIdentityProvider = Identity.OperatingSystemIdentityProvider.Create(this, CreationLifetime.Scoped);
        }

        internal static IIdentityService? Create(IApplicationBase? applicationBase) => applicationBase is null ? null : new IdentityService(applicationBase);
    }
}