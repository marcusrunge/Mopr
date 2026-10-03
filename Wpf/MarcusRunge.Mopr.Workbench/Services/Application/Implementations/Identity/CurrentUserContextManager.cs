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
        private readonly Lock _eventSynchronization = new();
        private CurrentUser? _currentUser;
        private Action<CurrentUser?>? _currentUserChanged;

        /// <inheritdoc/>
        public event Action<CurrentUser?> CurrentUserChanged
        {
            add
            {
                ArgumentNullException.ThrowIfNull(value);

                lock (_eventSynchronization)
                {
                    _currentUserChanged += value;
                }
            }
            remove
            {
                if (value is null)
                {
                    return;
                }

                lock (_eventSynchronization)
                {
                    _currentUserChanged -= value;
                }
            }
        }

        /// <inheritdoc/>
        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var previousUser = Interlocked.Exchange(ref _currentUser, null);

            // Repeated clearing must not produce redundant session notifications.
            if (previousUser is not null)
            {
                PublishCurrentUserChanged(currentUser: null);
            }

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

            if (user.Id <= 0 || !user.IsActive)
            {
                throw new InvalidOperationException("Only an active persistent MOPR user may be published to the current-user context.");
            }

            var previousUser = Interlocked.Exchange(ref _currentUser, user);

            // CurrentUser is immutable. Record equality prevents a duplicate
            // notification when the same session identity is published again.
            if (!Equals(previousUser, user))
            {
                PublishCurrentUserChanged(user);
            }

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

        private void PublishCurrentUserChanged(CurrentUser? currentUser)
        {
            Action<CurrentUser?>? handlers;

            lock (_eventSynchronization)
            {
                handlers = _currentUserChanged;
            }

            if (handlers is null)
            {
                return;
            }

            // A presentation subscriber must never invalidate an already completed
            // sign-in, provisioning operation or security-context transition.
            foreach (var handler in handlers.GetInvocationList().Cast<Action<CurrentUser?>>())
            {
                try
                {
                    handler(currentUser);
                }
                catch
                {
                    // Context publication remains authoritative even if an optional
                    // observer cannot update its presentation state.
                }
            }
        }
    }
}