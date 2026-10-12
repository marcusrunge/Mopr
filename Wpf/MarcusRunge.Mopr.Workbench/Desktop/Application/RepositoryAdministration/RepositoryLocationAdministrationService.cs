using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Application.RepositoryAdministration;

/// <summary>
/// Manages persisted repository locations without exposing Persistence entities to presentation modules.
/// </summary>
internal sealed class RepositoryLocationAdministrationService(IPersistence persistence, IApplication application) : IRepositoryLocationAdministrationService
{
    private readonly IApplication _application = application ?? throw new ArgumentNullException(nameof(application));
    private readonly IPersistence _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));

    /// <inheritdoc/>
    /// <inheritdoc/>
    public async Task<RepositoryLocationCreateResult> CreateAsync(RepositoryLocationCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RootPath);

        var currentUser = await (_application.IdentityService?.CurrentUserContext?.GetCurrentUserAsync(cancellationToken) ?? Task.FromResult<CurrentUser?>(null)).ConfigureAwait(false);
        if (currentUser is not { Id: > 0, IsActive: true })
        {
            return RepositoryLocationCreateResult.Unauthorized();
        }

        var repository = GetRepository();
        var normalizedName = request.Name.Trim();
        var normalizedRootPath = request.RootPath.Trim();

        if (await repository.GetByRootPathAsync(normalizedRootPath, cancellationToken).ConfigureAwait(false) is not null)
        {
            return RepositoryLocationCreateResult.DuplicatePath();
        }

        var location = new RepositoryLocation
        {
            Name = normalizedName,
            RootPath = normalizedRootPath,
            IsEnabled = request.IsDefault || request.IsEnabled,
            IsDefault = request.IsDefault,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = currentUser.Id
        };

        /*
         * AddAsync persists the entity but does not return it. Reloading the
         * location provides the database-generated identifier and RowVersion
         * required by the administration UI for subsequent updates.
         */
        await repository.AddAsync(location, cancellationToken).ConfigureAwait(false);

        var createdLocation = await repository.GetByRootPathAsync(normalizedRootPath, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Repository location '{normalizedRootPath}' was not available after successful creation.");

        return RepositoryLocationCreateResult.Success(Map(createdLocation));
    }

    /// <inheritdoc/>
    public async Task<RepositoryLocationDeleteResult> DeleteAsync(RepositoryLocationDeleteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateDeleteRequest(request);

        var currentUser = await (_application.IdentityService?.CurrentUserContext?.GetCurrentUserAsync(cancellationToken) ?? Task.FromResult<CurrentUser?>(null)).ConfigureAwait(false);
        if (currentUser is not { Id: > 0, IsActive: true })
        {
            return RepositoryLocationDeleteResult.Unauthorized();
        }

        var repository = GetRepository();
        var location = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (location is null)
        {
            return RepositoryLocationDeleteResult.NotFound();
        }

        if (!location.RowVersion.SequenceEqual(request.RowVersion))
        {
            return RepositoryLocationDeleteResult.Conflict();
        }

        /*
         * The default repository is an application invariant and must be replaced
         * explicitly before its configuration can be removed.
         */
        if (location.IsDefault)
        {
            return RepositoryLocationDeleteResult.DefaultRepository();
        }

        try
        {
            /*
             * Persistence owns the referential-integrity check. The repository-location
             * relationship uses restricted deletion, so referenced DICOM instances prevent
             * removal without exposing Persistence entities to the administration module.
             */
            await repository.DeleteAsync(location, cancellationToken).ConfigureAwait(false);
            return RepositoryLocationDeleteResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return RepositoryLocationDeleteResult.Conflict();
        }
        catch (DbUpdateException)
        {
            return RepositoryLocationDeleteResult.InUse();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RepositoryLocationSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var repository = GetRepository();
        var locations = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return [.. locations.OrderByDescending(location => location.IsDefault).ThenBy(location => location.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(location => location.RootPath, StringComparer.CurrentCultureIgnoreCase).Select(Map)];
    }

    /// <inheritdoc/>
    public async Task<RepositoryLocationUpdateResult> UpdateAsync(RepositoryLocationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequest(request);

        var currentUser = await (_application.IdentityService?.CurrentUserContext?.GetCurrentUserAsync(cancellationToken) ?? Task.FromResult<CurrentUser?>(null)).ConfigureAwait(false);
        if (currentUser is not { Id: > 0, IsActive: true })
        {
            return RepositoryLocationUpdateResult.Unauthorized();
        }

        var repository = GetRepository();
        var location = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (location is null)
        {
            return RepositoryLocationUpdateResult.NotFound();
        }

        if (!location.RowVersion.SequenceEqual(request.RowVersion))
        {
            return RepositoryLocationUpdateResult.Conflict();
        }

        // The only safe way to replace the current default is to mark another enabled
        // location as default. Directly clearing or disabling the current default would
        // leave Persistence in a state that MIRAS must classify as invalid.
        if (location.IsDefault && (!request.IsDefault || !request.IsEnabled) || request.IsDefault && !request.IsEnabled)
        {
            return RepositoryLocationUpdateResult.InvariantViolation();
        }

        var locationUsingPath = await repository.GetByRootPathAsync(request.RootPath, cancellationToken).ConfigureAwait(false);
        if (locationUsingPath is not null && locationUsingPath.Id != request.Id)
        {
            return RepositoryLocationUpdateResult.DuplicatePath();
        }

        location.Name = request.Name;
        location.RootPath = request.RootPath;
        location.IsEnabled = request.IsEnabled;
        location.IsDefault = request.IsDefault;
        location.ModifiedAtUtc = DateTime.UtcNow;
        location.ModifiedByUserId = currentUser.Id;

        try
        {
            await repository.UpdateAsync(location, cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return RepositoryLocationUpdateResult.Conflict();
        }

        var updatedLocation = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException($"Repository location '{request.Id}' was not available after a successful update.");
        return RepositoryLocationUpdateResult.Success(Map(updatedLocation));
    }

    private static RepositoryLocationSummary Map(RepositoryLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (location.RowVersion is not { Length: > 0 })
        {
            throw new InvalidOperationException($"Repository location '{location.Id}' does not have a valid concurrency token.");
        }

        return new RepositoryLocationSummary(location.Id, location.Name ?? string.Empty, location.RootPath ?? string.Empty, location.IsEnabled, location.IsDefault, [.. location.RowVersion]);
    }

    private static void ValidateDeleteRequest(RepositoryLocationDeleteRequest request)
    {
        if (request.Id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "The repository-location ID must be positive.");
        }

        if (request.RowVersion is not { Length: > 0 })
        {
            throw new ArgumentException("The repository-location concurrency token is required.", nameof(request));
        }
    }

    private static void ValidateRequest(RepositoryLocationUpdateRequest request)
    {
        if (request.Id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "The repository-location ID must be positive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RootPath);

        if (request.RowVersion is not { Length: > 0 })
        {
            throw new ArgumentException("The repository-location concurrency token is required.", nameof(request));
        }
    }

    private IRepositoryLocationRepository GetRepository() => _persistence.RepositoryLocation ?? throw new InvalidOperationException("The repository-location persistence service is unavailable.");
}