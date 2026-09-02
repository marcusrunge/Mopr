using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Resolves the persistent technical audit identity used for machine-wide setup changes.
    /// </summary>
    internal sealed class SetupAuditIdentityProvider(IPersistence persistence) : ISetupAuditIdentityProvider
    {
        private const string SystemFirstName = "MOPR";
        private const string SystemLastName = "System";
        private const string SystemLoginName = @"MOPR\SYSTEM";
        private const string SystemShortName = "SYSTEM";

        private readonly IPersistence _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));

        /// <inheritdoc/>
        public async Task<int> GetOrCreateUserIdAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var userRepository = _persistence.User ?? throw new InvalidOperationException("The Persistence user repository is not available.");

            var existingUser = await userRepository.GetByLoginNameAsync(SystemLoginName, cancellationToken).ConfigureAwait(false);
            if (existingUser is not null)
            {
                return ValidatePersistentUserId(existingUser);
            }

            var systemUser = CreateSystemUser();

            try
            {
                await userRepository.AddAsync(systemUser, cancellationToken).ConfigureAwait(false);
                return ValidatePersistentUserId(systemUser);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception creationException)
            {
                // Another workstation may have created the shared technical identity
                // after the initial lookup. Resolving it again distinguishes that safe
                // uniqueness race from an actual persistence failure.
                var concurrentlyCreatedUser = await TryGetExistingUserAsync(userRepository, cancellationToken).ConfigureAwait(false);
                if (concurrentlyCreatedUser is not null)
                {
                    return ValidatePersistentUserId(concurrentlyCreatedUser);
                }

                ExceptionDispatchInfo.Capture(creationException).Throw();
                throw;
            }
        }

        private static User CreateSystemUser() => new()
        {
            FirstName = SystemFirstName,
            LastName = SystemLastName,
            LoginName = SystemLoginName,
            ShortName = SystemShortName
        };

        private static int ValidatePersistentUserId(User user)
        {
            if (user.Id <= 0)
            {
                throw new InvalidOperationException("The technical setup audit identity does not have a valid persistent identifier.");
            }

            return user.Id;
        }

        private static async Task<User?> TryGetExistingUserAsync(IUserRepository userRepository, CancellationToken cancellationToken)
        {
            try
            {
                return await userRepository.GetByLoginNameAsync(SystemLoginName, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // The original creation failure remains the authoritative exception
                // when the verification lookup cannot confirm a uniqueness race.
                return null;
            }
        }
    }
}