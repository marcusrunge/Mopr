using MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Studies;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Provides the read-only persisted study and series catalog.
    /// </summary>
    public interface IStudyCatalogRepository
    {
        /// <summary>
        /// Gets all persisted studies with their series and aggregate image availability.
        /// </summary>
        /// <param name="cancellationToken">Cancels the read operation.</param>
        /// <returns>The persisted study catalog ordered by study date and patient name.</returns>
        Task<IReadOnlyList<PersistedStudyCatalogItem>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}