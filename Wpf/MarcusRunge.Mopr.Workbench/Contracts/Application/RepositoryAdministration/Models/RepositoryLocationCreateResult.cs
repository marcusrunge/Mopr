namespace MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryAdministration.Models;

/// <summary>
/// Describes the structured outcome of creating a repository location.
/// </summary>
public sealed class RepositoryLocationCreateResult
{
    private RepositoryLocationCreateResult(RepositoryLocationSummary? location, bool isDuplicatePath, bool isSuccess, bool isUnauthorized)
    {
        Location = location;
        IsDuplicatePath = isDuplicatePath;
        IsSuccess = isSuccess;
        IsUnauthorized = isUnauthorized;
    }

    /// <summary>Gets a value indicating whether another location already owns the requested root path.</summary>
    public bool IsDuplicatePath { get; }

    /// <summary>Gets a value indicating whether creation completed successfully.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether no persistent audit identity was available.</summary>
    public bool IsUnauthorized { get; }

    /// <summary>Gets the created location when creation succeeded.</summary>
    public RepositoryLocationSummary? Location { get; }

    /// <summary>Creates a duplicate-path result.</summary>
    public static RepositoryLocationCreateResult DuplicatePath() => new(null, true, false, false);

    /// <summary>Creates a successful result.</summary>
    public static RepositoryLocationCreateResult Success(RepositoryLocationSummary location) => new(location, false, true, false);

    /// <summary>Creates an unauthorized result.</summary>
    public static RepositoryLocationCreateResult Unauthorized() => new(null, false, false, true);
}
