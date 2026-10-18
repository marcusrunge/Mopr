using MarcusRunge.Mopr.Workbench.Services.Application.Models.UserAdministration;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.UserAdministration;

/// <summary>
/// Provides administrative access to persisted MOPR users.
/// </summary>
public interface IUserAdministrationService
{
    /// <summary>
    /// Creates one persistent personal MOPR user.
    /// </summary>
    /// <param name="request">The requested user values.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The structured creation result.</returns>
    Task<UserAdministrationCreateResult> CreateAsync(UserAdministrationCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every persisted MOPR user.
    /// </summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>Every persisted MOPR user.</returns>
    Task<IReadOnlyList<UserAdministrationSummary>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates one persisted MOPR user while preserving identity, security, and concurrency guarantees.
    /// </summary>
    /// <param name="request">The requested user update.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The structured update result.</returns>
    Task<UserAdministrationUpdateResult> UpdateAsync(UserAdministrationUpdateRequest request, CancellationToken cancellationToken = default);
}