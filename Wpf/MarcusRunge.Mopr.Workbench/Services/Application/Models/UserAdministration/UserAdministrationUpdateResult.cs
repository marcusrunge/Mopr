using MarcusRunge.Mopr.Workbench.Services.Application.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.UserAdministration;

/// <summary>
/// Describes the structured outcome of an administrative user update.
/// </summary>
public sealed class UserAdministrationUpdateResult
{
    private UserAdministrationUpdateResult(UserAdministrationUpdateStatus status, UserAdministrationSummary? user = null)
    {
        Status = status;
        User = user;
    }

    /// <summary>
    /// Gets a value indicating whether the update completed successfully.
    /// </summary>
    public bool IsSuccess => Status == UserAdministrationUpdateStatus.Completed;

    /// <summary>
    /// Gets the structured update status.
    /// </summary>
    public UserAdministrationUpdateStatus Status { get; }

    /// <summary>
    /// Gets the updated persistent user when the operation completed successfully.
    /// </summary>
    public UserAdministrationSummary? User { get; }

    /// <summary>
    /// Creates a concurrency-conflict result.
    /// </summary>
    public static UserAdministrationUpdateResult Conflict() => new(UserAdministrationUpdateStatus.Conflict);

    /// <summary>
    /// Creates a result for a duplicate operating-system login name.
    /// </summary>
    public static UserAdministrationUpdateResult DuplicateLoginName() => new(UserAdministrationUpdateStatus.DuplicateLoginName);

    /// <summary>
    /// Creates a result for a duplicate personnel number.
    /// </summary>
    public static UserAdministrationUpdateResult DuplicatePersonnelNumber() => new(UserAdministrationUpdateStatus.DuplicatePersonnelNumber);

    /// <summary>
    /// Creates a result for a duplicate Windows security identifier.
    /// </summary>
    public static UserAdministrationUpdateResult DuplicateSecurityIdentifier() => new(UserAdministrationUpdateStatus.DuplicateSecurityIdentifier);

    /// <summary>
    /// Creates a generic failure result.
    /// </summary>
    public static UserAdministrationUpdateResult Failed() => new(UserAdministrationUpdateStatus.Failed);

    /// <summary>
    /// Creates a not-found result.
    /// </summary>
    public static UserAdministrationUpdateResult NotFound() => new(UserAdministrationUpdateStatus.NotFound);

    /// <summary>
    /// Creates a result for a prohibited self-deactivation attempt.
    /// </summary>
    public static UserAdministrationUpdateResult SelfDeactivationNotAllowed() => new(UserAdministrationUpdateStatus.SelfDeactivationNotAllowed);

    /// <summary>
    /// Creates a result for a prohibited self-modification attempt.
    /// </summary>
    public static UserAdministrationUpdateResult SelfModificationNotAllowed() => new(UserAdministrationUpdateStatus.SelfModificationNotAllowed);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static UserAdministrationUpdateResult Success(UserAdministrationSummary user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserAdministrationUpdateResult(UserAdministrationUpdateStatus.Completed, user);
    }

    /// <summary>
    /// Creates a result for the protected technical system identity.
    /// </summary>
    public static UserAdministrationUpdateResult SystemUserProtected() => new(UserAdministrationUpdateStatus.SystemUserProtected);

    /// <summary>
    /// Creates an unauthorized result.
    /// </summary>
    public static UserAdministrationUpdateResult Unauthorized() => new(UserAdministrationUpdateStatus.Unauthorized);

    /// <summary>
    /// Creates a validation-failure result.
    /// </summary>
    public static UserAdministrationUpdateResult ValidationFailed() => new(UserAdministrationUpdateStatus.ValidationFailed);
}