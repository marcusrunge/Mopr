using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
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

            var operatingSystemIdentity = await operatingSystemIdentityProvider.GetCurrentIdentityAsync(cancellationToken).ConfigureAwait(false);

            if (operatingSystemIdentity is null)
            {
                return UserSignInResult.OperatingSystemIdentityUnavailable();
            }

            operatingSystemIdentity = NormalizeOperatingSystemIdentity(operatingSystemIdentity);

            var userRepository = applicationBase.Persistence?.User;

            if (userRepository is null)
            {
                return UserSignInResult.PersistenceUnavailable(operatingSystemIdentity);
            }

            User? persistentUser;

            try
            {
                persistentUser = await userRepository.GetByLoginNameAsync(operatingSystemIdentity.LoginName, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Technical details remain within the application-service error
                // channel. Callers receive only the stable sign-in status.
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

            CurrentUser currentUser;

            try
            {
                currentUser = CreateCurrentUser(persistentUser, operatingSystemIdentity.LoginName);
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

        private static CurrentUser CreateCurrentUser(User user, string resolvedLoginName)
        {
            ArgumentNullException.ThrowIfNull(user);

            return new CurrentUser(
                user.Id,
                resolvedLoginName,
                NormalizeRequiredValue(user.FirstName, nameof(user.FirstName)),
                NormalizeRequiredValue(user.LastName, nameof(user.LastName)),
                NormalizeRequiredValue(user.ShortName, nameof(user.ShortName)),
                user.IsActive);
        }

        private static OperatingSystemIdentity NormalizeOperatingSystemIdentity(OperatingSystemIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);

            return new OperatingSystemIdentity(NormalizeRequiredValue(identity.LoginName, nameof(identity.LoginName)));
        }

        private static string NormalizeRequiredValue(string? value, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"The persistent user property '{propertyName}' does not contain a usable value.");
            }

            return value.Trim();
        }
    }
}