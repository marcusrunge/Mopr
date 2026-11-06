using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies
{
    /// <summary>
    /// Provides the read-only application catalog of imported DICOM studies and series.
    /// </summary>
    public interface IStudyCatalogService
    {
        /// <summary>
        /// Gets all imported studies with their persisted series information.
        /// </summary>
        /// <param name="cancellationToken">Cancels the catalog operation.</param>
        /// <returns>The imported study catalog.</returns>
        Task<IReadOnlyList<StudyCatalogItem>> GetStudiesAsync(CancellationToken cancellationToken = default);
    }
}