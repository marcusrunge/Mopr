namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryAdministration;
/// <summary>Describes the structured outcome of an administrative repository-location update.</summary>
public sealed class RepositoryLocationUpdateResult
{
    private RepositoryLocationUpdateResult(RepositoryLocationSummary? location, bool isConflict, bool isDuplicatePath, bool isInvariantViolation, bool isNotFound, bool isSuccess, bool isUnauthorized) { Location = location; IsConflict = isConflict; IsDuplicatePath = isDuplicatePath; IsInvariantViolation = isInvariantViolation; IsNotFound = isNotFound; IsSuccess = isSuccess; IsUnauthorized = isUnauthorized; }
    public bool IsConflict { get; }
    public bool IsDuplicatePath { get; }
    public bool IsInvariantViolation { get; }
    public bool IsNotFound { get; }
    public bool IsSuccess { get; }
    public bool IsUnauthorized { get; }
    public RepositoryLocationSummary? Location { get; }
    public static RepositoryLocationUpdateResult Conflict() => new(null, true, false, false, false, false, false);
    public static RepositoryLocationUpdateResult DuplicatePath() => new(null, false, true, false, false, false, false);
    public static RepositoryLocationUpdateResult InvariantViolation() => new(null, false, false, true, false, false, false);
    public static RepositoryLocationUpdateResult NotFound() => new(null, false, false, false, true, false, false);
    public static RepositoryLocationUpdateResult Success(RepositoryLocationSummary location) => new(location, false, false, false, false, true, false);
    public static RepositoryLocationUpdateResult Unauthorized() => new(null, false, false, false, false, false, true);
}
