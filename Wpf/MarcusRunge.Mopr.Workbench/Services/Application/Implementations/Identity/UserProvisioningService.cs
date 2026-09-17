using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Identity
{
    /// <summary>
    /// Creates a persistent MOPR user for the current operating-system identity.
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

            // Any previous user is cleared before provisioning so failed or
            // canceled attempts cannot leave an unrelated identity authenticated.
            await contextManager.ClearAsync(cancellationToken).ConfigureAwait(false);

            var applicationBase = ((IServiceBase)Base).ApplicationBase ?? throw new InvalidOperationException("The application-service context is not available.");

            var operatingSystemIdentityProvider = applicationBase.OperatingSystemIdentityProvider;

            if (operatingSystemIdentityProvider is null)
            {
                return UserProvisioningResult.OperatingSystemIdentityUnavailable();
            }

            var operatingSystemIdentity = await operatingSystemIdentityProvider.GetCurrentIdentityAsync(cancellationToken).ConfigureAwait(false);

            if (operatingSystemIdentity is null)
            {
                return UserProvisioningResult.OperatingSystemIdentityUnavailable();
            }

            operatingSystemIdentity = new OperatingSystemIdentity(operatingSystemIdentity.LoginName.Trim());

            var userRepository = applicationBase.Persistence?.User;

            if (userRepository is null)
            {
                return UserProvisioningResult.PersistenceUnavailable(operatingSystemIdentity);
            }

            User? existingUser;

            try
            {
                existingUser = await userRepository.GetByLoginNameAsync(operatingSystemIdentity.LoginName, cancellationToken).ConfigureAwait(false);
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
                return await HandleExistingUserAsync(existingUser, operatingSystemIdentity, contextManager, applicationBase, cancellationToken).ConfigureAwait(false);
            }

            var user = new User
            {
                FirstName = validation.FirstName,
                IsActive = true,
                LastName = validation.LastName,
                LoginName = operatingSystemIdentity.LoginName,
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
                // Persistence-level uniqueness is authoritative for shared SQL
                // databases. A second lookup distinguishes the expected race from
                // an unrelated creation or database failure.
                var concurrentlyCreatedUser = await TryGetExistingUserAsync(userRepository, operatingSystemIdentity.LoginName, cancellationToken).ConfigureAwait(false);

                if (concurrentlyCreatedUser is not null)
                {
                    return await HandleExistingUserAsync(concurrentlyCreatedUser, operatingSystemIdentity, contextManager, applicationBase, cancellationToken).ConfigureAwait(false);
                }

                applicationBase.OnExceptionThrown(creationException);
                return UserProvisioningResult.Failed(operatingSystemIdentity);
            }

            if (user.Id <= 0)
            {
                applicationBase.OnExceptionThrown(new InvalidOperationException("The created MOPR user does not have a valid persistent identifier."));

                return UserProvisioningResult.Failed(operatingSystemIdentity);
            }

            var currentUser = CreateCurrentUser(user, operatingSystemIdentity.LoginName);
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

        private static CurrentUser CreateCurrentUser(User user, string resolvedLoginName)
        {
            if (user.Id <= 0)
            {
                throw new InvalidOperationException("The persistent MOPR user identifier must be positive.");
            }

            return new CurrentUser(user.Id, resolvedLoginName, RequirePersistentValue(user.FirstName, nameof(user.FirstName)), RequirePersistentValue(user.LastName, nameof(user.LastName)), RequirePersistentValue(user.ShortName, nameof(user.ShortName)), user.IsActive);
        }

        private static async Task<UserProvisioningResult> HandleExistingUserAsync(User user, OperatingSystemIdentity operatingSystemIdentity, ICurrentUserContextManager contextManager, IApplicationBase applicationBase, CancellationToken cancellationToken)
        {
            if (user.Id <= 0)
            {
                applicationBase.OnExceptionThrown(new InvalidOperationException("The existing MOPR user does not have a valid persistent identifier."));

                return UserProvisioningResult.Failed(operatingSystemIdentity);
            }

            CurrentUser currentUser;

            try
            {
                currentUser = CreateCurrentUser(user, operatingSystemIdentity.LoginName);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                applicationBase.OnExceptionThrown(exception);
                return UserProvisioningResult.Failed(operatingSystemIdentity);
            }

            // An active concurrently created user is published immediately so
            // startup can continue without requiring a second manual sign-in.
            if (currentUser.IsActive)
            {
                await contextManager.SetCurrentUserAsync(currentUser, cancellationToken).ConfigureAwait(false);
            }

            return UserProvisioningResult.UserAlreadyExists(operatingSystemIdentity, currentUser);
        }

        private static string RequirePersistentValue(string? value, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"The persistent user property '{propertyName}' does not contain a usable value.");
            }

            return value.Trim();
        }

        private static async Task<User?> TryGetExistingUserAsync(IUserRepository userRepository, string loginName, CancellationToken cancellationToken)
        {
            try
            {
                return await userRepository.GetByLoginNameAsync(loginName, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // The original creation failure remains authoritative when the
                // verification lookup cannot confirm a uniqueness race.
                return null;
            }
        }
    }
}