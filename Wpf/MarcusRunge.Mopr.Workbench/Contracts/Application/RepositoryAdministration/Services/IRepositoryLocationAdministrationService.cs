using MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Services
{
    /// <summary>
    /// Provides read-only administrative access to persisted repository locations.
    /// </summary>
    public interface IRepositoryLocationAdministrationService
    {
        /// <summary>
        /// Gets every persisted repository location, including disabled locations.
        /// </summary>
        Task<IReadOnlyList<RepositoryLocationSummary>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
