using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.UserAdministration.Models;
using MarcusRunge.Mopr.Workbench.Contracts.Application.UserAdministration.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Application.UserAdministration;

/// <summary>
/// Reads persisted MOPR users without exposing Persistence entities to presentation modules.
/// </summary>
internal sealed class UserAdministrationService(IPersistence persistence, IApplication application) : IUserAdministrationService
{
    private readonly IApplication _application = application ?? throw new ArgumentNullException(nameof(application));
    private readonly IPersistence _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UserAdministrationSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var repository = _persistence.User ?? throw new InvalidOperationException("The user Persistence service is unavailable.");
        var currentUser = await (_application.IdentityService?.CurrentUserContext?.GetCurrentUserAsync(cancellationToken) ?? Task.FromResult<CurrentUser?>(null)).ConfigureAwait(false);
        var users = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return [.. users.OrderByDescending(user => user.Id == currentUser?.Id).ThenByDescending(user => user.IsActive).ThenBy(user => user.LastName, StringComparer.CurrentCultureIgnoreCase).ThenBy(user => user.FirstName, StringComparer.CurrentCultureIgnoreCase).ThenBy(user => user.LoginName, StringComparer.CurrentCultureIgnoreCase).Select(user => Map(user, currentUser?.Id))];
    }

    private static UserAdministrationSummary Map(User user, int? currentUserId)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (user.RowVersion is not { Length: > 0 }) throw new InvalidOperationException($"User '{user.Id}' does not have a valid concurrency token.");
        return new UserAdministrationSummary(user.Id, user.LoginName ?? string.Empty, user.FirstName ?? string.Empty, user.MiddleName ?? string.Empty, user.LastName ?? string.Empty, user.ShortName ?? string.Empty, user.IsActive, user.Id == currentUserId, user.AcademicTitle ?? string.Empty, user.Suffix ?? string.Empty, user.PersonnelNumber ?? string.Empty, user.SecurityIdentifier ?? string.Empty, [.. user.RowVersion]);
    }
}
