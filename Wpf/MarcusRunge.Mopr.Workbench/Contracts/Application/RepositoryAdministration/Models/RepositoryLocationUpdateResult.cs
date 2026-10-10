namespace MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Models;

/// <summary>
/// Describes the structured outcome of an administrative repository-location update.
/// </summary>
public sealed class RepositoryLocationUpdateResult
{
    private RepositoryLocationUpdateResult(RepositoryLocationSummary? location, bool isConflict, bool isDuplicatePath, bool isInvariantViolation, bool isNotFound, bool isSuccess, bool isUnauthorized)
    {
        Location = location;
        IsConflict = isConflict;
        IsDuplicatePath = isDuplicatePath;
        IsInvariantViolation = isInvariantViolation;
        IsNotFound = isNotFound;
        IsSuccess = isSuccess;
        IsUnauthorized = isUnauthorized;
    }

    /// <summary>
    /// Gets a value indicating whether another process changed the location after it was loaded.
    /// </summary>
    public bool IsConflict { get; }

    /// <summary>
    /// Gets a value indicating whether another location already owns the requested root path.
    /// </summary>
    public bool IsDuplicatePath { get; }

    /// <summary>
    /// Gets a value indicating whether the requested state would violate a repository invariant.
    /// </summary>
    public bool IsInvariantViolation { get; }

    /// <summary>
    /// Gets a value indicating whether the requested location no longer exists.
    /// </summary>
    public bool IsNotFound { get; }

    /// <summary>
    /// Gets a value indicating whether the update completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether no valid persistent audit identity was available.
    /// </summary>
    public bool IsUnauthorized { get; }

    /// <summary>
    /// Gets the updated repository location when the operation succeeded.
    /// </summary>
    public RepositoryLocationSummary? Location { get; }

    /// <summary>
    /// Creates a concurrency-conflict result.
    /// </summary>
    public static RepositoryLocationUpdateResult Conflict() => new(null, true, false, false, false, false, false);

    /// <summary>
    /// Creates a duplicate-path result.
    /// </summary>
    public static RepositoryLocationUpdateResult DuplicatePath() => new(null, false, true, false, false, false, false);

    /// <summary>
    /// Creates an invariant-violation result.
    /// </summary>
    public static RepositoryLocationUpdateResult InvariantViolation() => new(null, false, false, true, false, false, false);

    /// <summary>
    /// Creates a not-found result.
    /// </summary>
    public static RepositoryLocationUpdateResult NotFound() => new(null, false, false, false, true, false, false);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static RepositoryLocationUpdateResult Success(RepositoryLocationSummary location) => new(location, false, false, false, false, true, false);

    /// <summary>
    /// Creates an unauthorized result.
    /// </summary>
    public static RepositoryLocationUpdateResult Unauthorized() => new(null, false, false, false, false, false, true);
}
