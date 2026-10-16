using MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryRecovery;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryRecovery;

/// <summary>
/// Performs explicit corrective actions for one configured repository location.
/// </summary>
public interface IRepositoryRecoveryActionService
{
    Task<RepositoryRecoveryActionResult> RecreateDirectoryAsync(int repositoryLocationId, string repositoryPath, CancellationToken cancellationToken = default);
    Task<RepositoryRecoveryActionResult> RelocateAsync(int repositoryLocationId, string name, string newRepositoryPath, bool isEnabled, bool isDefault, byte[] rowVersion, CancellationToken cancellationToken = default);
    Task<RepositoryRecoveryActionResult> RemoveAsync(int repositoryLocationId, byte[] rowVersion, CancellationToken cancellationToken = default);
}
