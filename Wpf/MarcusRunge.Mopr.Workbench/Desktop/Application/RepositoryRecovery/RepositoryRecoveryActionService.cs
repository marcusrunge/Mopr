using MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryRecovery;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;

namespace MarcusRunge.Mopr.Workbench.Application.RepositoryRecovery;

/// <summary>
/// Performs explicitly requested repository-location recovery actions through validated application contracts.
/// </summary>
internal sealed class RepositoryRecoveryActionService(IRepositoryLocationAdministrationService administrationService, IRepository repository, IAdministrativeAuthorizationService administrativeAuthorizationService) : IRepositoryRecoveryActionService
{
    private readonly IRepositoryLocationAdministrationService _administrationService = administrationService ?? throw new ArgumentNullException(nameof(administrationService));
    private readonly IRepositoryLocationValidationService _validationService = (repository ?? throw new ArgumentNullException(nameof(repository))).LocationValidation ?? throw new InvalidOperationException("The repository-location validation service has not been initialized.");
    private readonly IAdministrativeAuthorizationService _administrativeAuthorizationService = administrativeAuthorizationService ?? throw new ArgumentNullException(nameof(administrativeAuthorizationService));

    /// <inheritdoc/>
    public async Task<RepositoryRecoveryActionResult> RecreateDirectoryAsync(int repositoryLocationId, string repositoryPath, CancellationToken cancellationToken = default)
    {
        if (repositoryLocationId <= 0) throw new ArgumentOutOfRangeException(nameof(repositoryLocationId));
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);
        cancellationToken.ThrowIfCancellationRequested();
        if (!_administrativeAuthorizationService.IsElevatedAdministrator) return RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Unauthorized);

        try
        {
            Directory.CreateDirectory(repositoryPath);
            var validation = await _validationService.ValidateAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
            return validation.IsValid && !string.IsNullOrWhiteSpace(validation.NormalizedPath) ? RepositoryRecoveryActionResult.Completed(validation.NormalizedPath) : RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.ValidationFailed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Failed); }
    }

    /// <inheritdoc/>
    public async Task<RepositoryRecoveryActionResult> RelocateAsync(int repositoryLocationId, string name, string newRepositoryPath, bool isEnabled, bool isDefault, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        if (repositoryLocationId <= 0) throw new ArgumentOutOfRangeException(nameof(repositoryLocationId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(newRepositoryPath);
        if (rowVersion is not { Length: > 0 }) throw new ArgumentException("The repository-location concurrency token is required.", nameof(rowVersion));
        if (!_administrativeAuthorizationService.IsElevatedAdministrator) return RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Unauthorized);

        var validation = await _validationService.ValidateAsync(newRepositoryPath, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid || string.IsNullOrWhiteSpace(validation.NormalizedPath)) return RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.ValidationFailed);

        var result = await _administrationService.UpdateAsync(new RepositoryLocationUpdateRequest(repositoryLocationId, name, validation.NormalizedPath, isEnabled, isDefault, [.. rowVersion]), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? RepositoryRecoveryActionResult.Completed(validation.NormalizedPath) : result.IsConflict ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Conflict) : result.IsDuplicatePath ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.DuplicatePath) : result.IsNotFound ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.NotFound) : result.IsUnauthorized ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Unauthorized) : RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Failed);
    }

    /// <inheritdoc/>
    public async Task<RepositoryRecoveryActionResult> RemoveAsync(int repositoryLocationId, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        if (repositoryLocationId <= 0) throw new ArgumentOutOfRangeException(nameof(repositoryLocationId));
        if (rowVersion is not { Length: > 0 }) throw new ArgumentException("The repository-location concurrency token is required.", nameof(rowVersion));
        if (!_administrativeAuthorizationService.IsElevatedAdministrator) return RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Unauthorized);
        var result = await _administrationService.DeleteAsync(new RepositoryLocationDeleteRequest(repositoryLocationId, [.. rowVersion]), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? RepositoryRecoveryActionResult.Completed() : result.IsConflict ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Conflict) : result.IsDefaultRepository ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.DefaultRepository) : result.IsInUse ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.InUse) : result.IsNotFound ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.NotFound) : result.IsUnauthorized ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Unauthorized) : RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Failed);
    }
}
