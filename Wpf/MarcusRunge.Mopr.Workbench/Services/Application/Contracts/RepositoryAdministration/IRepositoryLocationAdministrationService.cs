using MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryAdministration;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.RepositoryAdministration;

/// <summary>Provides administrative access to persisted repository locations.</summary>
public interface IRepositoryLocationAdministrationService
{
    /// <summary>Creates a new repository location.</summary>
    /// <param name="request">The request to create a new repository location.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>The result of the creation operation.</returns>
    Task<RepositoryLocationCreateResult> CreateAsync(RepositoryLocationCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes a repository location.</summary>
    /// <param name="request">The request to delete a repository location.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>The result of the deletion operation.</returns>
    Task<RepositoryLocationDeleteResult> DeleteAsync(RepositoryLocationDeleteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets all repository locations.</summary>
    /// <param name="request">The request to get all repository locations.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>The list of repository locations.</returns>
    Task<IReadOnlyList<RepositoryLocationSummary>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Updates a repository location.</summary>
    /// <param name="request">The request to update a repository location.</param>
    /// <param name="cancellationToken">The token to cancel the operation.</param>
    /// <returns>The result of the update operation.</returns>
    Task<RepositoryLocationUpdateResult> UpdateAsync(RepositoryLocationUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a repository root path without changing persisted configuration.
    /// </summary>
    /// <param name="rootPath">The repository root path to validate.</param>
    /// <param name="cancellationToken">Cancels the validation.</param>
    /// <returns>The structured repository-location validation result.</returns>
    Task<RepositoryLocationValidationResult> ValidateAsync(string rootPath, CancellationToken cancellationToken = default);
}