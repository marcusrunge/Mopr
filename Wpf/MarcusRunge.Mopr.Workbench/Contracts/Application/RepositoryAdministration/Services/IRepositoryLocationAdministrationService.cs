using MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Services;

/// <summary>
/// Provides administrative access to persisted repository locations.
/// </summary>
public interface IRepositoryLocationAdministrationService
{
    /// <summary>
    /// Creates one persisted repository location.
    /// </summary>
    Task<RepositoryLocationCreateResult> CreateAsync(RepositoryLocationCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes one repository-location configuration without deleting physical repository content.
    /// </summary>
    Task<RepositoryLocationDeleteResult> DeleteAsync(RepositoryLocationDeleteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every persisted repository location, including disabled locations.
    /// </summary>
    Task<IReadOnlyList<RepositoryLocationSummary>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates one persisted repository location while preserving audit and concurrency guarantees.
    /// </summary>
    Task<RepositoryLocationUpdateResult> UpdateAsync(RepositoryLocationUpdateRequest request, CancellationToken cancellationToken = default);
}