using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Defines the contract for a repository that manages UnrealObject entities.
    /// </summary>
    public interface IUnrealObjectRepository : IRepository<UnrealObject>
    {       
        /// <summary>
        /// Gets all Unreal objects belonging to a specific DICOM instance.
        /// </summary>
        /// <param name="instanceId">The ID of the associated instance.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The Unreal objects associated with the specified instance.</returns>
        Task<IList<UnrealObject>> GetByInstanceIdAsync(int instanceId, CancellationToken cancellationToken = default);
    }
}