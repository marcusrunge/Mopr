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
    public async Task<IReadOnlyList<RepositoryLocationSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var repository = GetRepository();
        var locations = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return locations.OrderByDescending(location => location.IsDefault).ThenBy(location => location.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(location => location.RootPath, StringComparer.CurrentCultureIgnoreCase).Select(Map).ToArray();
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

    private IRepositoryLocationRepository GetRepository() => _persistence.RepositoryLocation ?? throw new InvalidOperationException("The repository-location persistence service is unavailable.");

    private static RepositoryLocationSummary Map(RepositoryLocation location) => new(location.Id, location.Name ?? string.Empty, location.RootPath ?? string.Empty, location.IsEnabled, location.IsDefault, [.. location.RowVersion]);

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
}
