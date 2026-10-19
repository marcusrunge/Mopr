using MarcusRunge.Mopr.Workbench.Services.Application.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.UserAdministration;

/// <summary>
/// Describes the structured outcome of administratively removing a persisted MOPR user.
/// </summary>
public sealed class UserAdministrationDeleteResult
{
    private UserAdministrationDeleteResult(UserAdministrationDeleteStatus status) => Status = status;

    /// <summary>
    /// Gets a value indicating whether the user was removed successfully.
    /// </summary>
    public bool IsSuccess => Status == UserAdministrationDeleteStatus.Completed;

    /// <summary>
    /// Gets the structured deletion status.
    /// </summary>
    public UserAdministrationDeleteStatus Status { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static UserAdministrationDeleteResult Success() => new(UserAdministrationDeleteStatus.Completed);

    /// <summary>
    /// Creates a concurrency-conflict result.
    /// </summary>
    public static UserAdministrationDeleteResult Conflict() => new(UserAdministrationDeleteStatus.Conflict);

    /// <summary>
    /// Creates a result for the protected current application-session identity.
    /// </summary>
    public static UserAdministrationDeleteResult CurrentUserProtected() => new(UserAdministrationDeleteStatus.CurrentUserProtected);

    /// <summary>
    /// Creates a generic failure result.
    /// </summary>
    public static UserAdministrationDeleteResult Failed() => new(UserAdministrationDeleteStatus.Failed);

    /// <summary>
    /// Creates a result for a user that is still referenced by persistent MOPR data.
    /// </summary>
    public static UserAdministrationDeleteResult InUse() => new(UserAdministrationDeleteStatus.InUse);

    /// <summary>
    /// Creates a not-found result.
    /// </summary>
    public static UserAdministrationDeleteResult NotFound() => new(UserAdministrationDeleteStatus.NotFound);

    /// <summary>
    /// Creates a result for the protected technical MOPR system identity.
    /// </summary>
    public static UserAdministrationDeleteResult SystemUserProtected() => new(UserAdministrationDeleteStatus.SystemUserProtected);

    /// <summary>
    /// Creates an unauthorized result.
    /// </summary>
    public static UserAdministrationDeleteResult Unauthorized() => new(UserAdministrationDeleteStatus.Unauthorized);

    /// <summary>
    /// Creates a validation-failure result.
    /// </summary>
    public static UserAdministrationDeleteResult ValidationFailed() => new(UserAdministrationDeleteStatus.ValidationFailed);
}