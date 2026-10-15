using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.UserAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.UserAdministration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.UserAdministration;

/// <summary>
/// Reads persisted MOPR users without exposing Persistence entities to presentation modules.
/// </summary>
internal sealed class UserAdministrationService : CreateableBindableBase<IUserAdministrationService, UserAdministrationService, IApplicationBase>, IUserAdministrationService
{
    private IApplicationBase? _base;

    private IApplicationBase Base => _base ?? throw new InvalidOperationException("The user-administration service has not been initialized.");

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UserAdministrationSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var repository = GetRepository();
        var currentUserId = Base.AuditIdentityProvider is { } auditIdentityProvider
            ? await auditIdentityProvider.GetCurrentUserIdAsync(cancellationToken).ConfigureAwait(false)
            : null;
        var users = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        return [.. users
            .OrderByDescending(user => user.Id == currentUserId)
            .ThenByDescending(user => user.IsActive)
            .ThenBy(user => user.LastName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(user => user.FirstName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(user => user.LoginName, StringComparer.CurrentCultureIgnoreCase)
            .Select(user => Map(user, currentUserId))];
    }

    /// <inheritdoc/>
    protected override void OnCreate(IApplicationBase @base) => _base = @base ?? throw new ArgumentNullException(nameof(@base));

    /// <inheritdoc/>
    protected override Task OnCreateAsync(IApplicationBase @base, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OnCreate(@base);
        return Task.CompletedTask;
    }

    private IUserRepository GetRepository() => Base.Persistence?.User ?? throw new InvalidOperationException("The user Persistence service is unavailable.");

    private static UserAdministrationSummary Map(User user, int? currentUserId)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.RowVersion is not { Length: > 0 })
        {
            throw new InvalidOperationException($"User '{user.Id}' does not have a valid concurrency token.");
        }

        return new UserAdministrationSummary(user.Id, user.LoginName ?? string.Empty, user.FirstName ?? string.Empty, user.MiddleName ?? string.Empty, user.LastName ?? string.Empty, user.ShortName ?? string.Empty, user.IsActive, user.Id == currentUserId, user.AcademicTitle ?? string.Empty, user.Suffix ?? string.Empty, user.PersonnelNumber ?? string.Empty, user.SecurityIdentifier ?? string.Empty, [.. user.RowVersion]);
    }
}
