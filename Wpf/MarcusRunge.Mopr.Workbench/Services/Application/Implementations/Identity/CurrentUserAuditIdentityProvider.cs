using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Identity
{
    /// <summary>
    /// Provides the persistent audit identity of the active signed-in MOPR user.
    /// </summary>
    internal sealed class CurrentUserAuditIdentityProvider : CreateableBindableBase<IAuditIdentityProvider, CurrentUserAuditIdentityProvider, IIdentityServiceBase>, IAuditIdentityProvider
    {
        private IIdentityServiceBase? _base;

        private IIdentityServiceBase Base => _base ?? throw new InvalidOperationException("The identity service has not been initialized.");

        /// <inheritdoc/>
        public async Task<int?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var context = Base.CurrentUserContextManager;

            if (context is null)
            {
                return null;
            }

            var user = await context.GetCurrentUserAsync(cancellationToken).ConfigureAwait(false);

            // Audit attribution is available only after a valid active user has
            // been signed in. Disabled or invalid users must never authorize
            // persisted business operations.
            return user is { Id: > 0, IsActive: true } ? user.Id : null;
        }

        /// <inheritdoc/>
        protected override void OnCreate(IIdentityServiceBase context) => _base = context ?? throw new ArgumentNullException(nameof(context));

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IIdentityServiceBase context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}