using MarcusRunge.Mopr.Workbench.Services.Application.Models.UserAdministration;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.UserAdministration;

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
