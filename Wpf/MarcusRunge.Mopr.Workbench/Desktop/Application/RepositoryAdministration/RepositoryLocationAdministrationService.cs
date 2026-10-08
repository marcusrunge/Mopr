using MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Services;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Application.RepositoryAdministration
{
    /// <summary>
    /// Reads persisted repository locations without exposing Persistence entities to the Setup module.
    /// </summary>
    internal sealed class RepositoryLocationAdministrationService(IPersistence persistence) : IRepositoryLocationAdministrationService
    {
        private readonly IPersistence _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));

        /// <inheritdoc/>
        public async Task<IReadOnlyList<RepositoryLocationSummary>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var repository = _persistence.RepositoryLocation ?? throw new InvalidOperationException("The repository-location persistence service is unavailable.");
            var locations = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
            return locations.OrderByDescending(location => location.IsDefault).ThenBy(location => location.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(location => location.RootPath, StringComparer.CurrentCultureIgnoreCase).Select(location => new RepositoryLocationSummary(location.Id, location.Name ?? string.Empty, location.RootPath ?? string.Empty, location.IsEnabled, location.IsDefault)).ToArray();
        }
    }
}
