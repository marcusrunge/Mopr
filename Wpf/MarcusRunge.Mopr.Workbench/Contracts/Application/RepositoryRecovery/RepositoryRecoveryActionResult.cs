namespace MarcusRunge.Mopr.Workbench.Contracts.Application.RepositoryRecovery;

/// <summary>
/// Represents the structured outcome of an explicit repository-location recovery action.
/// </summary>
public sealed record RepositoryRecoveryActionResult(RepositoryRecoveryActionStatus Status, string? NormalizedPath = null)
{
    public bool IsSuccess => Status == RepositoryRecoveryActionStatus.Completed;
    public static RepositoryRecoveryActionResult Completed(string? normalizedPath = null) => new(RepositoryRecoveryActionStatus.Completed, normalizedPath);
    public static RepositoryRecoveryActionResult FromStatus(RepositoryRecoveryActionStatus status) => new(status);
}
