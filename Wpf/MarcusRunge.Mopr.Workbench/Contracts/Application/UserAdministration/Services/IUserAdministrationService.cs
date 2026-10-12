using MarcusRunge.Mopr.Workbench.Contracts.Application.UserAdministration.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.UserAdministration.Services;

/// <summary>
/// Provides administrative read access to persisted MOPR users.
/// </summary>
public interface IUserAdministrationService
{
    /// <summary>
    /// Gets every persisted MOPR user.
    /// </summary>
    Task<IReadOnlyList<UserAdministrationSummary>> GetAllAsync(CancellationToken cancellationToken = default);
}
