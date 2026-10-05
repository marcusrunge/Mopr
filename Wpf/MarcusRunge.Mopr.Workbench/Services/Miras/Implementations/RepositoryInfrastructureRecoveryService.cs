using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Miras.Models;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Implementations
{
    /// <summary>
    /// Restores a missing machine-wide default repository location without
    /// rerunning or modifying the completed workstation setup.
    /// </summary>
    internal sealed class RepositoryInfrastructureRecoveryService : CreateableBindableBase<IRepositoryInfrastructureRecoveryService, RepositoryInfrastructureRecoveryService, IMirasBase>, IRepositoryInfrastructureRecoveryService
    {
        private const string DefaultRepositoryName = "Default DICOM repository";

        private IMirasBase? _base;

        private IMirasBase Base => _base ?? throw new InvalidOperationException("The MIRAS recovery service has not been initialized.");

        /// <inheritdoc/>
        public async Task<RepositoryInfrastructureRecoveryResult> RecoverAsync(RepositoryInfrastructureRecoveryRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            // Repository recovery changes machine-wide infrastructure. A valid
            // application user alone must never authorize this operation.
            if (!Base.AdministrativeAuthorizationService.IsElevatedAdministrator)
            {
                return RepositoryInfrastructureRecoveryResult.AdministrativeAuthorizationRequired();
            }

            var validationResult = await Base.RepositoryLocationValidationService.ValidateAsync(request.RepositoryPath ?? string.Empty, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (!validationResult.IsValid || string.IsNullOrWhiteSpace(validationResult.NormalizedPath))
            {
                return RepositoryInfrastructureRecoveryResult.RepositoryValidationFailed(validationResult.NormalizedPath);
            }

            var repositoryLocationRepository = Base.Persistence.RepositoryLocation;

            if (repositoryLocationRepository is null)
            {
                return RepositoryInfrastructureRecoveryResult.PersistenceUnavailable();
            }

            try
            {
                var existingLocations = await repositoryLocationRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                // This narrowly scoped recovery handles only complete loss of the
                // repository-location configuration. Existing rows require a
                // different MIRAS action and must never be silently overwritten.
                if (existingLocations.Count > 0)
                {
                    return RepositoryInfrastructureRecoveryResult.RepositoryAlreadyConfigured();
                }

                // The technical identity is reconstructed only after authorization,
                // path validation and the empty-set precondition have succeeded.
                var systemUserId = await Base.SystemAuditIdentityProvider.GetOrCreateUserIdAsync(cancellationToken).ConfigureAwait(false);

                if (systemUserId <= 0)
                {
                    throw new InvalidOperationException("The technical MOPR audit identity does not have a valid persistent identifier.");
                }

                var repositoryLocation = new RepositoryLocation
                {
                    CreatedByUserId = systemUserId,
                    IsDefault = true,
                    IsEnabled = true,
                    Name = DefaultRepositoryName,
                    RootPath = validationResult.NormalizedPath
                };

                await repositoryLocationRepository.AddAsync(repositoryLocation, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                if (repositoryLocation.Id <= 0)
                {
                    throw new InvalidOperationException("The restored repository location does not have a valid persistent identifier.");
                }

                return RepositoryInfrastructureRecoveryResult.Completed(repositoryLocation.Id, systemUserId, validationResult.NormalizedPath);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                Base.OnExceptionThrown(exception);
                return RepositoryInfrastructureRecoveryResult.Failed(exception);
            }
        }

        /// <inheritdoc/>
        protected override void OnCreate(IMirasBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IMirasBase @base, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}