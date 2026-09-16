using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Identity
{
    internal class OperatingSystemIdentityProvider : CreateableBindableBase<IOperatingSystemIdentityProvider, OperatingSystemIdentityProvider, IIdentityServiceBase>, IOperatingSystemIdentityProvider
    {
        private IIdentityServiceBase? _base;

        private IIdentityServiceBase Base => _base ?? throw new InvalidOperationException("The identity service has not been initialized.");

        public Task<OperatingSystemIdentity?> GetCurrentIdentityAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override void OnCreate(IIdentityServiceBase context) => _base = context ?? throw new ArgumentNullException(nameof(context));

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IIdentityServiceBase context, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}