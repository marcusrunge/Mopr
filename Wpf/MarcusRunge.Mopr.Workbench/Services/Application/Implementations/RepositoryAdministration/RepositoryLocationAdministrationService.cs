using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.RepositoryAdministration;

/// <summary>Manages persisted repository locations without exposing Persistence entities to presentation modules.</summary>
internal sealed class RepositoryLocationAdministrationService : CreateableBindableBase<IRepositoryLocationAdministrationService, RepositoryLocationAdministrationService, IApplicationBase>, IRepositoryLocationAdministrationService
{
    private IApplicationBase? _base;
    private IApplicationBase Base => _base ?? throw new InvalidOperationException("The repository-location administration service has not been initialized.");

    public async Task<RepositoryLocationCreateResult> CreateAsync(RepositoryLocationCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request); cancellationToken.ThrowIfCancellationRequested(); ArgumentException.ThrowIfNullOrWhiteSpace(request.Name); ArgumentException.ThrowIfNullOrWhiteSpace(request.RootPath);
        var auditUserId = await GetAuditUserIdAsync(cancellationToken).ConfigureAwait(false);
        if (!auditUserId.HasValue) return RepositoryLocationCreateResult.Unauthorized();
        var repository = GetRepository(); var normalizedName = request.Name.Trim(); var normalizedRootPath = request.RootPath.Trim();
        if (await repository.GetByRootPathAsync(normalizedRootPath, cancellationToken).ConfigureAwait(false) is not null) return RepositoryLocationCreateResult.DuplicatePath();
        var location = new RepositoryLocation { Name = normalizedName, RootPath = normalizedRootPath, IsEnabled = request.IsDefault || request.IsEnabled, IsDefault = request.IsDefault, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = auditUserId.Value };
        await repository.AddAsync(location, cancellationToken).ConfigureAwait(false);
        var createdLocation = await repository.GetByRootPathAsync(normalizedRootPath, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException($"Repository location '{normalizedRootPath}' was not available after successful creation.");
        return RepositoryLocationCreateResult.Success(Map(createdLocation));
    }

    public async Task<RepositoryLocationDeleteResult> DeleteAsync(RepositoryLocationDeleteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request); cancellationToken.ThrowIfCancellationRequested(); ValidateDeleteRequest(request);
        return await DeleteCoreAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<RepositoryLocationDeleteResult> DeleteCoreAsync(RepositoryLocationDeleteRequest request, CancellationToken cancellationToken)
    {
        var auditUserId = await GetAuditUserIdAsync(cancellationToken).ConfigureAwait(false);
        if (!auditUserId.HasValue) return RepositoryLocationDeleteResult.Unauthorized();
        var repository = GetRepository(); var location = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (location is null) return RepositoryLocationDeleteResult.NotFound();
        if (!location.RowVersion.SequenceEqual(request.RowVersion)) return RepositoryLocationDeleteResult.Conflict();
        if (location.IsDefault) return RepositoryLocationDeleteResult.DefaultRepository();
        try { await repository.DeleteAsync(location, cancellationToken).ConfigureAwait(false); return RepositoryLocationDeleteResult.Success(); }
        catch (DbUpdateConcurrencyException) { return RepositoryLocationDeleteResult.Conflict(); }
        catch (DbUpdateException) { return RepositoryLocationDeleteResult.InUse(); }
    }

    public async Task<IReadOnlyList<RepositoryLocationSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var locations = await GetRepository().GetAllAsync(cancellationToken).ConfigureAwait(false);
        return [.. locations.OrderByDescending(location => location.IsDefault).ThenBy(location => location.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(location => location.RootPath, StringComparer.CurrentCultureIgnoreCase).Select(Map)];
    }

    public async Task<RepositoryLocationUpdateResult> UpdateAsync(RepositoryLocationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request); cancellationToken.ThrowIfCancellationRequested(); ValidateRequest(request);
        var auditUserId = await GetAuditUserIdAsync(cancellationToken).ConfigureAwait(false);
        if (!auditUserId.HasValue) return RepositoryLocationUpdateResult.Unauthorized();
        var repository = GetRepository(); var location = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (location is null) return RepositoryLocationUpdateResult.NotFound();
        if (!location.RowVersion.SequenceEqual(request.RowVersion)) return RepositoryLocationUpdateResult.Conflict();
        if (location.IsDefault && (!request.IsDefault || !request.IsEnabled) || request.IsDefault && !request.IsEnabled) return RepositoryLocationUpdateResult.InvariantViolation();
        var locationUsingPath = await repository.GetByRootPathAsync(request.RootPath, cancellationToken).ConfigureAwait(false);
        if (locationUsingPath is not null && locationUsingPath.Id != request.Id) return RepositoryLocationUpdateResult.DuplicatePath();
        location.Name = request.Name; location.RootPath = request.RootPath; location.IsEnabled = request.IsEnabled; location.IsDefault = request.IsDefault; location.ModifiedAtUtc = DateTime.UtcNow; location.ModifiedByUserId = auditUserId.Value;
        try { await repository.UpdateAsync(location, cancellationToken).ConfigureAwait(false); }
        catch (DbUpdateConcurrencyException) { return RepositoryLocationUpdateResult.Conflict(); }
        var updatedLocation = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException($"Repository location '{request.Id}' was not available after a successful update.");
        return RepositoryLocationUpdateResult.Success(Map(updatedLocation));
    }

    protected override void OnCreate(IApplicationBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));
    protected override Task OnCreateAsync(IApplicationBase @base, CancellationToken cancellationToken) { cancellationToken.ThrowIfCancellationRequested(); OnCreate(@base); return Task.CompletedTask; }
    private async Task<int?> GetAuditUserIdAsync(CancellationToken cancellationToken) => Base.AuditIdentityProvider is { } provider ? await provider.GetCurrentUserIdAsync(cancellationToken).ConfigureAwait(false) : null;
    private IRepositoryLocationRepository GetRepository() => Base.Persistence?.RepositoryLocation ?? throw new InvalidOperationException("The repository-location persistence service is unavailable.");
    private static RepositoryLocationSummary Map(RepositoryLocation location) { ArgumentNullException.ThrowIfNull(location); if (location.RowVersion is not { Length: > 0 }) throw new InvalidOperationException($"Repository location '{location.Id}' does not have a valid concurrency token."); return new(location.Id, location.Name ?? string.Empty, location.RootPath ?? string.Empty, location.IsEnabled, location.IsDefault, [.. location.RowVersion]); }
    private static void ValidateDeleteRequest(RepositoryLocationDeleteRequest request) { if (request.Id <= 0) throw new ArgumentOutOfRangeException(nameof(request), "The repository-location ID must be positive."); if (request.RowVersion is not { Length: > 0 }) throw new ArgumentException("The repository-location concurrency token is required.", nameof(request)); }
    private static void ValidateRequest(RepositoryLocationUpdateRequest request) { if (request.Id <= 0) throw new ArgumentOutOfRangeException(nameof(request), "The repository-location ID must be positive."); ArgumentException.ThrowIfNullOrWhiteSpace(request.Name); ArgumentException.ThrowIfNullOrWhiteSpace(request.RootPath); if (request.RowVersion is not { Length: > 0 }) throw new ArgumentException("The repository-location concurrency token is required.", nameof(request)); }
}
