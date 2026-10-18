using MarcusRunge.Mopr.Workbench.Services.Application.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.UserAdministration;

/// <summary>
/// Describes the structured outcome of administratively creating a MOPR user.
/// </summary>
public sealed class UserAdministrationCreateResult
{
    private UserAdministrationCreateResult(UserAdministrationCreateStatus status, UserAdministrationSummary? user = null)
    {
        Status = status;
        User = user;
    }

    /// <summary>
    /// Gets a value indicating whether the user was created successfully.
    /// </summary>
    public bool IsSuccess => Status == UserAdministrationCreateStatus.Completed;

    /// <summary>
    /// Gets the structured creation status.
    /// </summary>
    public UserAdministrationCreateStatus Status { get; }

    /// <summary>
    /// Gets the created persistent user when the operation completed successfully.
    /// </summary>
    public UserAdministrationSummary? User { get; }

    /// <summary>
    /// Creates a concurrency-conflict result.
    /// </summary>
    public static UserAdministrationCreateResult Conflict() => new(UserAdministrationCreateStatus.Conflict);

    /// <summary>
    /// Creates a result for a duplicate operating-system login name.
    /// </summary>
    public static UserAdministrationCreateResult DuplicateLoginName() => new(UserAdministrationCreateStatus.DuplicateLoginName);

    /// <summary>
    /// Creates a result for a duplicate personnel number.
    /// </summary>
    public static UserAdministrationCreateResult DuplicatePersonnelNumber() => new(UserAdministrationCreateStatus.DuplicatePersonnelNumber);

    /// <summary>
    /// Creates a result for a duplicate Windows security identifier.
    /// </summary>
    public static UserAdministrationCreateResult DuplicateSecurityIdentifier() => new(UserAdministrationCreateStatus.DuplicateSecurityIdentifier);

    /// <summary>
    /// Creates a generic failure result.
    /// </summary>
    public static UserAdministrationCreateResult Failed() => new(UserAdministrationCreateStatus.Failed);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static UserAdministrationCreateResult Success(UserAdministrationSummary user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserAdministrationCreateResult(UserAdministrationCreateStatus.Completed, user);
    }

    /// <summary>
    /// Creates a result for the reserved technical MOPR system identity.
    /// </summary>
    public static UserAdministrationCreateResult SystemUserReserved() => new(UserAdministrationCreateStatus.SystemUserReserved);

    /// <summary>
    /// Creates an unauthorized result.
    /// </summary>
    public static UserAdministrationCreateResult Unauthorized() => new(UserAdministrationCreateStatus.Unauthorized);

    /// <summary>
    /// Creates a validation-failure result.
    /// </summary>
    public static UserAdministrationCreateResult ValidationFailed() => new(UserAdministrationCreateStatus.ValidationFailed);
}