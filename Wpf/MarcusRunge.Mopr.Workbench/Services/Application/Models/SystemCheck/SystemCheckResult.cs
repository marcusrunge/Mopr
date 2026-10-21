using MarcusRunge.Mopr.Workbench.Services.Application.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.SystemCheck;

/// <summary>
/// Describes one complete read-only system-check run.
/// </summary>
/// <param name="Status">The aggregated status of the complete run.</param>
/// <param name="StartedAtUtc">The UTC timestamp at which the run started.</param>
/// <param name="CompletedAtUtc">The UTC timestamp at which the run completed.</param>
/// <param name="Database">The database-connection result.</param>
/// <param name="Persistence">The Persistence-integrity result.</param>
/// <param name="Repository">The MIRAS Repository result.</param>
public sealed record SystemCheckResult(SystemCheckSectionStatus Status, DateTimeOffset StartedAtUtc, DateTimeOffset CompletedAtUtc, SystemCheckSectionResult Database, SystemCheckSectionResult Persistence, SystemCheckSectionResult Repository)
{
    /// <summary>
    /// Gets the elapsed duration of the complete run.
    /// </summary>
    public TimeSpan Duration => CompletedAtUtc - StartedAtUtc;

    /// <summary>
    /// Gets a value indicating whether the complete run finished without a blocking or unavailable section.
    /// </summary>
    public bool IsSuccessful => Database.IsSuccessful && Persistence.IsSuccessful && Repository.IsSuccessful;
}
