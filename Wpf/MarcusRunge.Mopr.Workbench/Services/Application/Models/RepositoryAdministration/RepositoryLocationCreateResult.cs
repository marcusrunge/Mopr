namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryAdministration;
/// <summary>Describes the structured outcome of creating a repository location.</summary>
public sealed class RepositoryLocationCreateResult
{
    private RepositoryLocationCreateResult(RepositoryLocationSummary? location, bool isDuplicatePath, bool isSuccess, bool isUnauthorized) { Location = location; IsDuplicatePath = isDuplicatePath; IsSuccess = isSuccess; IsUnauthorized = isUnauthorized; }
    public bool IsDuplicatePath { get; }
    public bool IsSuccess { get; }
    public bool IsUnauthorized { get; }
    public RepositoryLocationSummary? Location { get; }
    public static RepositoryLocationCreateResult DuplicatePath() => new(null, true, false, false);
    public static RepositoryLocationCreateResult Success(RepositoryLocationSummary location) => new(location, false, true, false);
    public static RepositoryLocationCreateResult Unauthorized() => new(null, false, false, true);
}
