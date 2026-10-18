using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.UserAdministration;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.UserAdministration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.UserAdministration;

/// <summary>
/// Manages persisted MOPR users without exposing Persistence entities to presentation modules.
/// </summary>
internal sealed class UserAdministrationService : CreateableBindableBase<IUserAdministrationService, UserAdministrationService, IApplicationBase>, IUserAdministrationService
{
    private IApplicationBase? _base;

    private IApplicationBase Base => _base ?? throw new InvalidOperationException("The user-administration service has not been initialized.");

    /// <inheritdoc/>
    public async Task<UserAdministrationCreateResult> CreateAsync(UserAdministrationCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryNormalizeRequest(request, out var normalizedRequest)) return UserAdministrationCreateResult.ValidationFailed();
        if (string.Equals(normalizedRequest.LoginName, WellKnownUserLoginNames.System, StringComparison.OrdinalIgnoreCase)) return UserAdministrationCreateResult.SystemUserReserved();

        var currentUserId = await GetCurrentUserIdAsync(cancellationToken).ConfigureAwait(false);
        if (currentUserId is null or <= 0) return UserAdministrationCreateResult.Unauthorized();

        var repository = GetRepository();

        if (await repository.GetByLoginNameAsync(normalizedRequest.LoginName, cancellationToken).ConfigureAwait(false) is not null) return UserAdministrationCreateResult.DuplicateLoginName();
        if (normalizedRequest.SecurityIdentifier is not null && await repository.GetBySecurityIdentifierAsync(normalizedRequest.SecurityIdentifier, cancellationToken).ConfigureAwait(false) is not null) return UserAdministrationCreateResult.DuplicateSecurityIdentifier();

        var users = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        if (normalizedRequest.PersonnelNumber is not null && users.Any(user => EqualsNormalized(user.PersonnelNumber, normalizedRequest.PersonnelNumber))) return UserAdministrationCreateResult.DuplicatePersonnelNumber();

        var user = new User
        {
            LoginName = normalizedRequest.LoginName,
            FirstName = normalizedRequest.FirstName,
            MiddleName = normalizedRequest.MiddleName,
            LastName = normalizedRequest.LastName,
            ShortName = normalizedRequest.ShortName,
            AcademicTitle = normalizedRequest.AcademicTitle,
            Suffix = normalizedRequest.Suffix,
            PersonnelNumber = normalizedRequest.PersonnelNumber,
            SecurityIdentifier = normalizedRequest.SecurityIdentifier,
            IsActive = normalizedRequest.IsActive
        };

        try
        {
            await repository.AddAsync(user, cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            /*
             * A concurrent creation may pass the initial uniqueness checks but still
             * violate a database uniqueness constraint. The structured conflict result
             * prevents database details from leaking into the administration module.
             */
            return UserAdministrationCreateResult.Conflict();
        }

        var createdUser = await repository.GetByLoginNameAsync(normalizedRequest.LoginName, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException($"User '{normalizedRequest.LoginName}' was not available after successful creation.");
        return UserAdministrationCreateResult.Success(Map(createdUser, currentUserId));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UserAdministrationSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var repository = GetRepository();
        var currentUserId = await GetCurrentUserIdAsync(cancellationToken).ConfigureAwait(false);
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
    public async Task<UserAdministrationUpdateResult> UpdateAsync(UserAdministrationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryNormalizeRequest(request, out var normalizedRequest)) return UserAdministrationUpdateResult.ValidationFailed();

        var currentUserId = await GetCurrentUserIdAsync(cancellationToken).ConfigureAwait(false);
        if (currentUserId is null or <= 0) return UserAdministrationUpdateResult.Unauthorized();

        var repository = GetRepository();
        var user = await repository.GetByIdAsync(normalizedRequest.Id, cancellationToken).ConfigureAwait(false);

        if (user is null) return UserAdministrationUpdateResult.NotFound();
        if (user.RowVersion is not { Length: > 0 } || !user.RowVersion.SequenceEqual(normalizedRequest.RowVersion)) return UserAdministrationUpdateResult.Conflict();
        if (IsSystemUser(user)) return UserAdministrationUpdateResult.SystemUserProtected();
        if (user.Id == currentUserId && !normalizedRequest.IsActive) return UserAdministrationUpdateResult.SelfDeactivationNotAllowed();

        if (user.Id == currentUserId)
        {
            var machineConfigurationService = Base.MachineConfigurationService;
            if (machineConfigurationService is null) return UserAdministrationUpdateResult.Failed();

            var configuration = await machineConfigurationService.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (!configuration.Security.AllowSelfModification) return UserAdministrationUpdateResult.SelfModificationNotAllowed();
        }

        var users = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var duplicateStatus = GetDuplicateStatus(users, normalizedRequest);

        if (duplicateStatus is not null) return duplicateStatus;

        ApplyUpdate(user, normalizedRequest);

        try
        {
            await repository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return UserAdministrationUpdateResult.Conflict();
        }
        catch (DbUpdateException)
        {
            return UserAdministrationUpdateResult.Failed();
        }

        var updatedUser = await repository.GetByIdAsync(user.Id, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException($"User '{user.Id}' was not available after a successful update.");
        return UserAdministrationUpdateResult.Success(Map(updatedUser, currentUserId));
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

    private static void ApplyUpdate(User user, UserAdministrationUpdateRequest request)
    {
        /*
         * Identity values are applied only after all security, uniqueness, and
         * concurrency checks have completed, so failed requests cannot partially
         * modify the tracked Persistence entity.
         */
        user.LoginName = request.LoginName;
        user.FirstName = request.FirstName;
        user.MiddleName = request.MiddleName;
        user.LastName = request.LastName;
        user.ShortName = request.ShortName;
        user.AcademicTitle = request.AcademicTitle;
        user.Suffix = request.Suffix;
        user.PersonnelNumber = request.PersonnelNumber;
        user.SecurityIdentifier = request.SecurityIdentifier;
        user.IsActive = request.IsActive;
    }

    private async Task<int?> GetCurrentUserIdAsync(CancellationToken cancellationToken) => Base.AuditIdentityProvider is { } auditIdentityProvider ? await auditIdentityProvider.GetCurrentUserIdAsync(cancellationToken).ConfigureAwait(false) : null;

    private static UserAdministrationUpdateResult? GetDuplicateStatus(IEnumerable<User> users, UserAdministrationUpdateRequest request)
    {
        foreach (var user in users)
        {
            if (user.Id == request.Id) continue;
            if (EqualsNormalized(user.LoginName, request.LoginName)) return UserAdministrationUpdateResult.DuplicateLoginName();
            if (request.SecurityIdentifier is not null && EqualsNormalized(user.SecurityIdentifier, request.SecurityIdentifier)) return UserAdministrationUpdateResult.DuplicateSecurityIdentifier();
            if (request.PersonnelNumber is not null && EqualsNormalized(user.PersonnelNumber, request.PersonnelNumber)) return UserAdministrationUpdateResult.DuplicatePersonnelNumber();
        }

        return null;
    }

    private IUserRepository GetRepository() => Base.Persistence?.User ?? throw new InvalidOperationException("The user Persistence service is unavailable.");

    private static bool IsSystemUser(User user) => string.Equals(user.LoginName?.Trim(), WellKnownUserLoginNames.System, StringComparison.OrdinalIgnoreCase);

    private static UserAdministrationSummary Map(User user, int? currentUserId)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.RowVersion is not { Length: > 0 }) throw new InvalidOperationException($"User '{user.Id}' does not have a valid concurrency token.");

        return new UserAdministrationSummary(user.Id, user.LoginName ?? string.Empty, user.FirstName ?? string.Empty, user.MiddleName ?? string.Empty, user.LastName ?? string.Empty, user.ShortName ?? string.Empty, user.IsActive, user.Id == currentUserId, user.AcademicTitle ?? string.Empty, user.Suffix ?? string.Empty, user.PersonnelNumber ?? string.Empty, user.SecurityIdentifier ?? string.Empty, [.. user.RowVersion]);
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeSecurityIdentifier(string? value) => NormalizeOptional(value)?.ToUpperInvariant();

    private static bool TryNormalizeRequest(UserAdministrationCreateRequest request, out UserAdministrationCreateRequest normalizedRequest)
    {
        normalizedRequest = request;

        if (string.IsNullOrWhiteSpace(request.LoginName) || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.ShortName)) return false;

        normalizedRequest = request with
        {
            LoginName = request.LoginName.Trim(),
            FirstName = request.FirstName.Trim(),
            MiddleName = NormalizeOptional(request.MiddleName),
            LastName = request.LastName.Trim(),
            ShortName = request.ShortName.Trim(),
            AcademicTitle = NormalizeOptional(request.AcademicTitle),
            Suffix = NormalizeOptional(request.Suffix),
            PersonnelNumber = NormalizeOptional(request.PersonnelNumber),
            SecurityIdentifier = NormalizeSecurityIdentifier(request.SecurityIdentifier)
        };

        return true;
    }

    private static bool TryNormalizeRequest(UserAdministrationUpdateRequest request, out UserAdministrationUpdateRequest normalizedRequest)
    {
        normalizedRequest = request;

        if (request.Id <= 0 || request.RowVersion is not { Length: > 0 } || string.IsNullOrWhiteSpace(request.LoginName) || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.ShortName)) return false;

        normalizedRequest = request with
        {
            LoginName = request.LoginName.Trim(),
            FirstName = request.FirstName.Trim(),
            MiddleName = NormalizeOptional(request.MiddleName),
            LastName = request.LastName.Trim(),
            ShortName = request.ShortName.Trim(),
            AcademicTitle = NormalizeOptional(request.AcademicTitle),
            Suffix = NormalizeOptional(request.Suffix),
            PersonnelNumber = NormalizeOptional(request.PersonnelNumber),
            SecurityIdentifier = NormalizeSecurityIdentifier(request.SecurityIdentifier),
            RowVersion = [.. request.RowVersion]
        };

        return true;
    }

    private static bool EqualsNormalized(string? left, string? right) => string.Equals(NormalizeOptional(left), NormalizeOptional(right), StringComparison.OrdinalIgnoreCase);
}