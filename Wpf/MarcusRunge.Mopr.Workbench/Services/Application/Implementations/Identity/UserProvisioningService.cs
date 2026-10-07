using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Identity
{
    /// <summary>
    /// Creates the first persistent personal MOPR user for the current operating-system identity.
    /// </summary>
    internal sealed class UserProvisioningService : CreateableBindableBase<IUserProvisioningService, UserProvisioningService, IIdentityServiceBase>, IUserProvisioningService
    {
        private IIdentityServiceBase? _base;

        private IIdentityServiceBase Base => _base ?? throw new InvalidOperationException("The identity service has not been initialized.");

        /// <inheritdoc/>
        public async Task<UserProvisioningResult> ProvisionAsync(UserProvisioningRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            var validation = UserProvisioningValidator.Validate(request);

            if (!validation.IsValid)
            {
                return UserProvisioningResult.ValidationFailed(validation.Issues);
            }

            var contextManager = Base.CurrentUserContextManager ?? throw new InvalidOperationException("The current-user context manager is not available.");
            var applicationBase = ((IServiceBase)Base).ApplicationBase ?? throw new InvalidOperationException("The application-service context is not available.");
            var administrativeAuthorizationService = applicationBase.AdministrativeAuthorizationService;

            /*
             * Initial user provisioning changes the persistent authorization
             * boundary. Authorization must be checked before the active user
             * context is cleared so a denied request cannot terminate a valid
             * existing application session.
             */
            if (administrativeAuthorizationService?.IsElevatedAdministrator != true)
            {
                return UserProvisioningResult.AdministrativeAuthorizationRequired();
            }

            /*
             * After authorization has succeeded, a previous identity must not
             * remain active while provisioning is unresolved or unsuccessful.
             */
            await contextManager.ClearAsync(cancellationToken).ConfigureAwait(false);

            var operatingSystemIdentityProvider = applicationBase.OperatingSystemIdentityProvider;

            if (operatingSystemIdentityProvider is null)
            {
                return UserProvisioningResult.OperatingSystemIdentityUnavailable();
            }

            var resolvedIdentity = await operatingSystemIdentityProvider.GetCurrentIdentityAsync(cancellationToken).ConfigureAwait(false);

            if (resolvedIdentity is null)
            {
                return UserProvisioningResult.OperatingSystemIdentityUnavailable();
            }

            var operatingSystemIdentity = NormalizeOperatingSystemIdentity(resolvedIdentity);

            if (operatingSystemIdentity.SecurityIdentifier is null)
            {
                return UserProvisioningResult.OperatingSystemIdentityUnavailable();
            }

            var userRepository = applicationBase.Persistence?.User;

            if (userRepository is null)
            {
                return UserProvisioningResult.PersistenceUnavailable(operatingSystemIdentity);
            }

            User? existingUser;

            try
            {
                existingUser = await ResolvePersistentUserAsync(userRepository, operatingSystemIdentity, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                applicationBase.OnExceptionThrown(exception);
                return UserProvisioningResult.Failed(operatingSystemIdentity);
            }

            if (existingUser is not null)
            {
                return await HandleExistingUserAsync(userRepository, existingUser, operatingSystemIdentity, contextManager, applicationBase, cancellationToken).ConfigureAwait(false);
            }

            bool hasPersonalUsers;

            try
            {
                hasPersonalUsers = await userRepository.HasPersonalUsersAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                applicationBase.OnExceptionThrown(exception);
                return UserProvisioningResult.Failed(operatingSystemIdentity);
            }

            if (hasPersonalUsers)
            {
                /*
                 * This service may create only the first personal user. Additional
                 * users require the dedicated administrative user-management flow.
                 */
                applicationBase.OnExceptionThrown(new InvalidOperationException("Initial user provisioning is unavailable because a personal MOPR user already exists."));
                return UserProvisioningResult.Failed(operatingSystemIdentity);
            }

            var user = new User
            {
                AcademicTitle = validation.AcademicTitle,
                FirstName = validation.FirstName,
                IsActive = true,
                LastName = validation.LastName,
                LoginName = operatingSystemIdentity.LoginName,
                PersonnelNumber = validation.PersonnelNumber,
                SecurityIdentifier = operatingSystemIdentity.SecurityIdentifier,
                ShortName = validation.ShortName
            };

            try
            {
                await userRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception creationException)
            {
                /*
                 * A competing MOPR instance may have provisioned this Windows
                 * account after the initial lookup. The SID remains authoritative
                 * when the competing record is resolved.
                 */
                var concurrentlyCreatedUser = await TryGetExistingUserAsync(userRepository, operatingSystemIdentity, cancellationToken).ConfigureAwait(false);

                if (concurrentlyCreatedUser is not null)
                {
                    return await HandleExistingUserAsync(userRepository, concurrentlyCreatedUser, operatingSystemIdentity, contextManager, applicationBase, cancellationToken).ConfigureAwait(false);
                }

                applicationBase.OnExceptionThrown(creationException);
                return UserProvisioningResult.Failed(operatingSystemIdentity);
            }

            if (user.Id <= 0)
            {
                applicationBase.OnExceptionThrown(new InvalidOperationException("The created MOPR user does not have a valid persistent identifier."));
                return UserProvisioningResult.Failed(operatingSystemIdentity);
            }

            var currentUser = CurrentUserMapper.Map(user, operatingSystemIdentity.LoginName);
            await contextManager.SetCurrentUserAsync(currentUser, cancellationToken).ConfigureAwait(false);

            return UserProvisioningResult.Completed(operatingSystemIdentity, currentUser);
        }

        /// <inheritdoc/>
        protected override void OnCreate(IIdentityServiceBase context) => _base = context ?? throw new ArgumentNullException(nameof(context));

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IIdentityServiceBase context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        private static async Task<UserProvisioningResult> HandleExistingUserAsync(IUserRepository userRepository, User user, OperatingSystemIdentity operatingSystemIdentity, ICurrentUserContextManager contextManager, IApplicationBase applicationBase, CancellationToken cancellationToken)
        {
            try
            {
                user = await SynchronizePersistentIdentityAsync(userRepository, user, operatingSystemIdentity, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                /*
                 * Provisioning must not publish a user whose durable Windows
                 * assignment could not be confirmed or persisted.
                 */
                applicationBase.OnExceptionThrown(exception);
                return UserProvisioningResult.Failed(operatingSystemIdentity);
            }

            CurrentUser currentUser;

            try
            {
                currentUser = CurrentUserMapper.Map(user, operatingSystemIdentity.LoginName);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                applicationBase.OnExceptionThrown(exception);
                return UserProvisioningResult.Failed(operatingSystemIdentity);
            }

            if (!currentUser.IsActive)
            {
                return UserProvisioningResult.UserDisabled(operatingSystemIdentity, currentUser);
            }

            /*
             * A matching active user created by a competing application instance
             * can become the current user without repeating provisioning.
             */
            await contextManager.SetCurrentUserAsync(currentUser, cancellationToken).ConfigureAwait(false);

            return UserProvisioningResult.UserAlreadyExists(operatingSystemIdentity, currentUser);
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
             * Login-name fallback exists only for migration of a user created
             * before SID assignment was introduced. A record that already owns
             * another SID must never be reassigned because Windows reused a name.
             */
            var userByLoginName = await userRepository.GetByLoginNameAsync(operatingSystemIdentity.LoginName, cancellationToken).ConfigureAwait(false);

            return userByLoginName is null || !string.IsNullOrWhiteSpace(userByLoginName.SecurityIdentifier)
                ? null
                : userByLoginName;
        }

        private static async Task<User> SynchronizePersistentIdentityAsync(IUserRepository userRepository, User persistentUser, OperatingSystemIdentity operatingSystemIdentity, CancellationToken cancellationToken)
        {
            var securityIdentifier = operatingSystemIdentity.SecurityIdentifier ?? throw new InvalidOperationException("The operating-system identity does not contain a Windows security identifier.");
            var persistentSecurityIdentifier = NormalizeSecurityIdentifier(persistentUser.SecurityIdentifier);

            if (persistentSecurityIdentifier is not null && !string.Equals(persistentSecurityIdentifier, securityIdentifier, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The persistent MOPR user is assigned to a different Windows security identifier.");
            }

            var loginNameChanged = !string.Equals(persistentUser.LoginName, operatingSystemIdentity.LoginName, StringComparison.OrdinalIgnoreCase);
            var securityIdentifierChanged = persistentSecurityIdentifier is null;

            if (!loginNameChanged && !securityIdentifierChanged)
            {
                return persistentUser;
            }

            /*
             * The SID is the durable assignment. Login-name synchronization is
             * allowed only after a matching SID or a safe legacy fallback has
             * confirmed ownership of the persistent user.
             */
            persistentUser.LoginName = operatingSystemIdentity.LoginName;
            persistentUser.SecurityIdentifier = securityIdentifier;

            await userRepository.UpdateAsync(persistentUser, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            return persistentUser;
        }

        private static async Task<User?> TryGetExistingUserAsync(IUserRepository userRepository, OperatingSystemIdentity operatingSystemIdentity, CancellationToken cancellationToken)
        {
            try
            {
                var securityIdentifier = operatingSystemIdentity.SecurityIdentifier ?? throw new InvalidOperationException("The operating-system identity does not contain a Windows security identifier.");
                var userBySecurityIdentifier = await userRepository.GetBySecurityIdentifierAsync(securityIdentifier, cancellationToken).ConfigureAwait(false);

                if (userBySecurityIdentifier is not null)
                {
                    return userBySecurityIdentifier;
                }

                var userByLoginName = await userRepository.GetByLoginNameAsync(operatingSystemIdentity.LoginName, cancellationToken).ConfigureAwait(false);

                return userByLoginName is null || !string.IsNullOrWhiteSpace(userByLoginName.SecurityIdentifier)
                    ? null
                    : userByLoginName;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                /*
                 * The original creation failure remains authoritative when the
                 * verification lookup cannot confirm a safe uniqueness race.
                 */
                return null;
            }
        }
    }
}