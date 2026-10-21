using MarcusRunge.Mopr.Workbench.Services.Application.Models.SystemCheck;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.SystemCheck;

/// <summary>
/// Coordinates a read-only check of the configured MOPR workstation services.
/// </summary>
public interface ISystemCheckService
{
    /// <summary>
    /// Runs the complete read-only system check.
    /// </summary>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The user-oriented aggregated check result.</returns>
    Task<SystemCheckResult> RunAsync(CancellationToken cancellationToken = default);
}
