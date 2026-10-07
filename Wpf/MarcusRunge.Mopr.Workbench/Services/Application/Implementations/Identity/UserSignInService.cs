using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Identity
{
    /// <summary>
    /// Resolves the current operating-system identity to an existing persistent MOPR user.
    /// </summary>
    internal sealed class UserSignInService : CreateableBindableBase<IUserSignInService, UserSignInService, IIdentityServiceBase>, IUserSignInService
    {
        private IIdentityServiceBase? _base;

        private IIdentityServiceBase Base => _base ?? throw new InvalidOperationException("The identity service has not been initialized.");

        /// <inheritdoc/>
        public async Task<UserSignInResult> SignInAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var contextManager = Base.CurrentUserContextManager ?? throw new InvalidOperationException("The current-user context manager is not available.");

            // A previous identity must never remain active while a new sign-in
            // attempt is unresolved, canceled or rejected.
            await contextManager.ClearAsync(cancellationToken).ConfigureAwait(false);

            var applicationBase = ((IServiceBase)Base).ApplicationBase ?? throw new InvalidOperationException("The application-service context is not available.");
            var operatingSystemIdentityProvider = applicationBase.OperatingSystemIdentityProvider;

            if (operatingSystemIdentityProvider is null)
            {
                return UserSignInResult.OperatingSystemIdentityUnavailable();
            }

            var resolvedIdentity = await operatingSystemIdentityProvider.GetCurrentIdentityAsync(cancellationToken).ConfigureAwait(false);

            if (resolvedIdentity is null)
            {
                return UserSignInResult.OperatingSystemIdentityUnavailable();
            }

            var operatingSystemIdentity = NormalizeOperatingSystemIdentity(resolvedIdentity);

            if (operatingSystemIdentity.SecurityIdentifier is null)
            {
                return UserSignInResult.OperatingSystemIdentityUnavailable();
            }

            var userRepository = applicationBase.Persistence?.User;

            if (userRepository is null)
            {
                return UserSignInResult.PersistenceUnavailable(operatingSystemIdentity);
            }

            User? persistentUser;

            try
            {
                persistentUser = await ResolvePersistentUserAsync(userRepository, operatingSystemIdentity, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Technical details remain in the internal application error
                // channel and must not become user-facing sign-in messages.
                applicationBase.OnExceptionThrown(exception);
                return UserSignInResult.Failed(operatingSystemIdentity);
            }

            if (persistentUser is null)
            {
                return UserSignInResult.UserUnknown(operatingSystemIdentity);
            }

            if (persistentUser.Id <= 0)
            {
                return UserSignInResult.InvalidPersistentUserId(operatingSystemIdentity);
            }

            try
            {
                persistentUser = await SynchronizePersistentIdentityAsync(userRepository, persistentUser, operatingSystemIdentity, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                /*
                 * A user must not be authenticated with stale or incomplete account
                 * assignment data if the SID migration or login-name synchronization
                 * could not be persisted.
                 */
                applicationBase.OnExceptionThrown(exception);
                return UserSignInResult.Failed(operatingSystemIdentity);
            }

            CurrentUser currentUser;

            try
            {
                currentUser = CurrentUserMapper.Map(persistentUser, operatingSystemIdentity.LoginName);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                // Incomplete persisted names represent invalid user data and must
                // never be converted into an authenticated application context.
                applicationBase.OnExceptionThrown(exception);
                return UserSignInResult.Failed(operatingSystemIdentity);
            }

            if (!currentUser.IsActive)
            {
                return UserSignInResult.UserDisabled(operatingSystemIdentity, currentUser);
            }

            await contextManager.SetCurrentUserAsync(currentUser, cancellationToken).ConfigureAwait(false);
            return UserSignInResult.SignedIn(operatingSystemIdentity, currentUser);
        }

        /// <inheritdoc/>
        protected override void OnCreate(IIdentityServiceBase context) => _base = context ?? throw new ArgumentNullException(nameof(context));

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IIdentityServiceBase context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        private static OperatingSystemIdentity NormalizeOperatingSystemIdentity(OperatingSystemIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);

            var loginName = IdentityValueNormalizer.NormalizeLoginName(identity.LoginName);
            var securityIdentifier = NormalizeSecurityIdentifier(identity.SecurityIdentifier);

            return new OperatingSystemIdentity(loginName, securityIdentifier);
        }

        private static string? NormalizeSecurityIdentifier(string? securityIdentifier) => string.IsNullOrWhiteSpace(securityIdentifier) ? null : securityIdentifier.Trim();

        private static async Task<User?> ResolvePersistentUserAsync(IUserRepository userRepository, OperatingSystemIdentity operatingSystemIdentity, CancellationToken cancellationToken)
        {
            var securityIdentifier = operatingSystemIdentity.SecurityIdentifier ?? throw new InvalidOperationException("The operating-system identity does not contain a Windows security identifier.");
            var userBySecurityIdentifier = await userRepository.GetBySecurityIdentifierAsync(securityIdentifier, cancellationToken).ConfigureAwait(false);

            if (userBySecurityIdentifier is not null)
            {
                return userBySecurityIdentifier;
            }

            /*
             * Login-name fallback exists only for migration of an existing user
             * created before SID assignment was introduced. An existing different
             * SID must never be replaced merely because Windows reused a login name.
             */
            var userByLoginName = await userRepository.GetByLoginNameAsync(operatingSystemIdentity.LoginName, cancellationToken).ConfigureAwait(false);

            return userByLoginName is null || !string.IsNullOrWhiteSpace(userByLoginName.SecurityIdentifier)
                ? null
                : userByLoginName;
        }

        private static async Task<User> SynchronizePersistentIdentityAsync(IUserRepository userRepository, User persistentUser, OperatingSystemIdentity operatingSystemIdentity, CancellationToken cancellationToken)
        {
            var securityIdentifier = operatingSystemIdentity.SecurityIdentifier ?? throw new InvalidOperationException("The operating-system identity does not contain a Windows security identifier.");
            var normalizedPersistentSecurityIdentifier = NormalizeSecurityIdentifier(persistentUser.SecurityIdentifier);

            if (normalizedPersistentSecurityIdentifier is not null && !string.Equals(normalizedPersistentSecurityIdentifier, securityIdentifier, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The persistent MOPR user is assigned to a different Windows security identifier.");
            }

            var loginNameChanged = !string.Equals(persistentUser.LoginName, operatingSystemIdentity.LoginName, StringComparison.OrdinalIgnoreCase);
            var securityIdentifierChanged = normalizedPersistentSecurityIdentifier is null;

            if (!loginNameChanged && !securityIdentifierChanged)
            {
                return persistentUser;
            }

            /*
             * The SID is the durable identity. The login name is synchronized only
             * after a matching SID or a safe legacy login-name fallback established
             * that the persisted record belongs to the current Windows account.
             */
            persistentUser.LoginName = operatingSystemIdentity.LoginName;
            persistentUser.SecurityIdentifier = securityIdentifier;

            await userRepository.UpdateAsync(persistentUser, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            return persistentUser;
        }
    }
}