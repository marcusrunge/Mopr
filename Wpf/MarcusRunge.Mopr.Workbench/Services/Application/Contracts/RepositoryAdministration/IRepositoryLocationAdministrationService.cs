using MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryAdministration;
namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryAdministration;
/// <summary>Provides administrative access to persisted repository locations.</summary>
public interface IRepositoryLocationAdministrationService
{
    Task<RepositoryLocationCreateResult> CreateAsync(RepositoryLocationCreateRequest request, CancellationToken cancellationToken = default);
    Task<RepositoryLocationDeleteResult> DeleteAsync(RepositoryLocationDeleteRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RepositoryLocationSummary>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RepositoryLocationUpdateResult> UpdateAsync(RepositoryLocationUpdateRequest request, CancellationToken cancellationToken = default);
}
