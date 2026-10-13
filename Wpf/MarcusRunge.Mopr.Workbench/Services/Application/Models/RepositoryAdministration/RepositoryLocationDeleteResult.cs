namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryAdministration;
/// <summary>Describes the structured outcome of deleting one repository-location configuration.</summary>
public sealed class RepositoryLocationDeleteResult
{
    private RepositoryLocationDeleteResult(bool isConflict, bool isDefaultRepository, bool isInUse, bool isNotFound, bool isSuccess, bool isUnauthorized) { IsConflict = isConflict; IsDefaultRepository = isDefaultRepository; IsInUse = isInUse; IsNotFound = isNotFound; IsSuccess = isSuccess; IsUnauthorized = isUnauthorized; }
    public bool IsConflict { get; }
    public bool IsDefaultRepository { get; }
    public bool IsInUse { get; }
    public bool IsNotFound { get; }
    public bool IsSuccess { get; }
    public bool IsUnauthorized { get; }
    public static RepositoryLocationDeleteResult Conflict() => new(true, false, false, false, false, false);
    public static RepositoryLocationDeleteResult DefaultRepository() => new(false, true, false, false, false, false);
    public static RepositoryLocationDeleteResult InUse() => new(false, false, true, false, false, false);
    public static RepositoryLocationDeleteResult NotFound() => new(false, false, false, true, false, false);
    public static RepositoryLocationDeleteResult Success() => new(false, false, false, false, true, false);
    public static RepositoryLocationDeleteResult Unauthorized() => new(false, false, false, false, false, true);
}
