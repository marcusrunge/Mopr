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

            // A previous user must not remain authenticated while provisioning
            // another operating-system identity is incomplete or unsuccessful.
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

            operatingSystemIdentity = new OperatingSystemIdentity(IdentityValueNormalizer.NormalizeLoginName(operatingSystemIdentity.LoginName));

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

            var user = new User { FirstName = validation.FirstName, IsActive = true, LastName = validation.LastName, LoginName = operatingSystemIdentity.LoginName, ShortName = validation.ShortName };

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
                // The unique LoginName index remains authoritative across all
                // application instances sharing the configured SQL database.
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

        private static async Task<UserProvisioningResult> HandleExistingUserAsync(User user, OperatingSystemIdentity operatingSystemIdentity, ICurrentUserContextManager contextManager, IApplicationBase applicationBase, CancellationToken cancellationToken)
        {
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

            // A concurrently created active user can become the current user
            // immediately without requiring a second provisioning operation.
            if (currentUser.IsActive)
            {
                await contextManager.SetCurrentUserAsync(currentUser, cancellationToken).ConfigureAwait(false);
            }

            return UserProvisioningResult.UserAlreadyExists(operatingSystemIdentity, currentUser);
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
                // The original creation exception remains authoritative when the
                // verification lookup cannot confirm a concurrent creation.
                return null;
            }
        }
    }
}