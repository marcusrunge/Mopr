using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Defines the contract for a repository that manages Measurement entities.
    /// </summary>
    public interface IMeasurementRepository : IRepository<Measurement>
    {
        /// <summary>
        /// Gets all measurements belonging to a specific instance.
        /// </summary>
        /// <param name="instanceId">The ID of the instance.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of measurements for the specified instance.</returns>
        Task<IList<Measurement>> GetByInstanceIdAsync(int instanceId, CancellationToken cancellationToken = default);
    }
}