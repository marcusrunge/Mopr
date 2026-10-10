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
    /// Gets every persisted repository location, including disabled locations.
    /// </summary>
    Task<IReadOnlyList<RepositoryLocationSummary>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates one persisted repository location while preserving audit and concurrency guarantees.
    /// </summary>
    Task<RepositoryLocationUpdateResult> UpdateAsync(RepositoryLocationUpdateRequest request, CancellationToken cancellationToken = default);
}
