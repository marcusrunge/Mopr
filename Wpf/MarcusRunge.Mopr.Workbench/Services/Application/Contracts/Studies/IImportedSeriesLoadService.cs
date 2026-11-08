using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies
{
    /// <summary>
    /// Resolves one imported DICOM series to completely validated repository files.
    /// </summary>
    public interface IImportedSeriesLoadService
    {
        /// <summary>
        /// Resolves and validates every persisted file belonging to the requested series.
        /// </summary>
        /// <param name="request">The selected series identity.</param>
        /// <param name="cancellationToken">Cancels the resolution operation.</param>
        /// <returns>A successful result only when every persisted instance is safe and physically readable.</returns>
        Task<ImportedSeriesLoadResult> ResolveAsync(ImportedSeriesLoadRequest request, CancellationToken cancellationToken = default);
    }
}