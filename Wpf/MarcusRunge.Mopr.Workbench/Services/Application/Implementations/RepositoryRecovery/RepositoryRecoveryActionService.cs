using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryRecovery;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryRecovery;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using System.IO;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.RepositoryRecovery;

/// <summary>
/// Performs explicitly requested repository-location recovery actions through validated application contracts.
/// </summary>
internal sealed class RepositoryRecoveryActionService : CreateableBindableBase<IRepositoryRecoveryActionService, RepositoryRecoveryActionService, IApplicationBase>, IRepositoryRecoveryActionService
{
    private IApplicationBase? _base;

    private IApplicationBase Base => _base ?? throw new InvalidOperationException("The repository-recovery action service has not been initialized.");

    /// <inheritdoc/>
    public async Task<RepositoryRecoveryActionResult> RecreateDirectoryAsync(int repositoryLocationId, string repositoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(repositoryLocationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsElevatedAdministrator())
        {
            return RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Unauthorized);
        }

        try
        {
            Directory.CreateDirectory(repositoryPath);
            var validation = await GetValidationService().ValidateAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
            return validation.IsValid && !string.IsNullOrWhiteSpace(validation.NormalizedPath) ? RepositoryRecoveryActionResult.Completed(validation.NormalizedPath) : RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.ValidationFailed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Failed);
        }
    }

    /// <inheritdoc/>
    public async Task<RepositoryRecoveryActionResult> RelocateAsync(int repositoryLocationId, string name, string newRepositoryPath, bool isEnabled, bool isDefault, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(repositoryLocationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(newRepositoryPath);
        if (rowVersion is not { Length: > 0 }) throw new ArgumentException("The repository-location concurrency token is required.", nameof(rowVersion));
        if (!IsElevatedAdministrator()) return RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Unauthorized);

        var validation = await GetValidationService().ValidateAsync(newRepositoryPath, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid || string.IsNullOrWhiteSpace(validation.NormalizedPath)) return RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.ValidationFailed);

        var result = await GetAdministrationService().UpdateAsync(new RepositoryLocationUpdateRequest(repositoryLocationId, name, validation.NormalizedPath, isEnabled, isDefault, [.. rowVersion]), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? RepositoryRecoveryActionResult.Completed(validation.NormalizedPath) : result.IsConflict ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Conflict) : result.IsDuplicatePath ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.DuplicatePath) : result.IsNotFound ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.NotFound) : result.IsUnauthorized ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Unauthorized) : RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Failed);
    }

    /// <inheritdoc/>
    public async Task<RepositoryRecoveryActionResult> RemoveAsync(int repositoryLocationId, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(repositoryLocationId);
        if (rowVersion is not { Length: > 0 }) throw new ArgumentException("The repository-location concurrency token is required.", nameof(rowVersion));
        if (!IsElevatedAdministrator()) return RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Unauthorized);

        var result = await GetAdministrationService().DeleteAsync(new RepositoryLocationDeleteRequest(repositoryLocationId, [.. rowVersion]), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? RepositoryRecoveryActionResult.Completed() : result.IsConflict ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Conflict) : result.IsDefaultRepository ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.DefaultRepository) : result.IsInUse ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.InUse) : result.IsNotFound ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.NotFound) : result.IsUnauthorized ? RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Unauthorized) : RepositoryRecoveryActionResult.FromStatus(RepositoryRecoveryActionStatus.Failed);
    }

    /// <inheritdoc/>
    protected override void OnCreate(IApplicationBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

    /// <inheritdoc/>
    protected override Task OnCreateAsync(IApplicationBase @base, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OnCreate(@base);
        return Task.CompletedTask;
    }

    private Contracts.RepositoryAdministration.IRepositoryLocationAdministrationService GetAdministrationService() => Base.RepositoryAdministration ?? throw new InvalidOperationException("The repository-location administration service has not been initialized.");

    private IRepositoryLocationValidationService GetValidationService() => Base.Repository?.LocationValidation ?? throw new InvalidOperationException("The repository-location validation service has not been initialized.");

    private bool IsElevatedAdministrator() => Base.AdministrativeAuthorizationService?.IsElevatedAdministrator == true;
}
