using MarcusRunge.Base;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Defines the contract for a generic repository.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IRepository<TEntity> where TEntity : BindableEntityBase
    {
        /// <summary>
        /// Adds a new entity.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all persisted entities of this repository type.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>All persisted entities.</returns>
        Task<IList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an entity by its ID.
        /// </summary>
        /// <param name="id">The ID of the entity to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The entity if found; otherwise, null.</returns>
        Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    }
}