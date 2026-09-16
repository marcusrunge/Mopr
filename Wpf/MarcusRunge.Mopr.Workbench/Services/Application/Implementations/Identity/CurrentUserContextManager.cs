using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Identity
{
    /// <summary>
    /// Manages the persistent MOPR user assigned to one identity-service instance.
    /// </summary>
    internal sealed class CurrentUserContextManager : CreateableBindableBase<ICurrentUserContextManager, CurrentUserContextManager, IIdentityServiceBase>, ICurrentUserContextManager
    {
        private CurrentUser? _currentUser;

        /// <inheritdoc/>
        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _currentUser, null);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<CurrentUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // CurrentUser is immutable, so an atomic reference read provides a
            // complete and internally consistent snapshot to every consumer.
            return Task.FromResult(Volatile.Read(ref _currentUser));
        }

        /// <inheritdoc/>
        public Task SetCurrentUserAsync(CurrentUser user, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(user);

            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _currentUser, user);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override void OnCreate(IIdentityServiceBase context)
        {
        }

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IIdentityServiceBase context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}