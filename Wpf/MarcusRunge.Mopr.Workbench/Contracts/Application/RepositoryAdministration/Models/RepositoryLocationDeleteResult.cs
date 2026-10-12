namespace MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Models;

/// <summary>
/// Describes the structured outcome of deleting one repository-location configuration.
/// </summary>
public sealed class RepositoryLocationDeleteResult
{
    private RepositoryLocationDeleteResult(bool isConflict, bool isDefaultRepository, bool isInUse, bool isNotFound, bool isSuccess, bool isUnauthorized)
    {
        IsConflict = isConflict;
        IsDefaultRepository = isDefaultRepository;
        IsInUse = isInUse;
        IsNotFound = isNotFound;
        IsSuccess = isSuccess;
        IsUnauthorized = isUnauthorized;
    }

    /// <summary>
    /// Gets a value indicating whether the location changed after it was loaded.
    /// </summary>
    public bool IsConflict { get; }

    /// <summary>
    /// Gets a value indicating whether the location is the current default repository.
    /// </summary>
    public bool IsDefaultRepository { get; }

    /// <summary>
    /// Gets a value indicating whether persisted DICOM instances still reference the location.
    /// </summary>
    public bool IsInUse { get; }

    /// <summary>
    /// Gets a value indicating whether the requested location no longer exists.
    /// </summary>
    public bool IsNotFound { get; }

    /// <summary>
    /// Gets a value indicating whether the configuration was deleted successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether no valid persistent audit identity was available.
    /// </summary>
    public bool IsUnauthorized { get; }

    /// <summary>
    /// Creates a concurrency-conflict result.
    /// </summary>
    public static RepositoryLocationDeleteResult Conflict() => new(true, false, false, false, false, false);

    /// <summary>
    /// Creates a result for the protected default repository.
    /// </summary>
    public static RepositoryLocationDeleteResult DefaultRepository() => new(false, true, false, false, false, false);

    /// <summary>
    /// Creates a result for a repository location that is still referenced by DICOM data.
    /// </summary>
    public static RepositoryLocationDeleteResult InUse() => new(false, false, true, false, false, false);

    /// <summary>
    /// Creates a not-found result.
    /// </summary>
    public static RepositoryLocationDeleteResult NotFound() => new(false, false, false, true, false, false);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static RepositoryLocationDeleteResult Success() => new(false, false, false, false, true, false);

    /// <summary>
    /// Creates an unauthorized result.
    /// </summary>
    public static RepositoryLocationDeleteResult Unauthorized() => new(false, false, false, false, false, true);
}