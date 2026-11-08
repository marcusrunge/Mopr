using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Studies
{
    /// <summary>
    /// Resolves imported DICOM series to completely validated repository files.
    /// </summary>
    public interface IImportedSeriesLoadService
    {
        /// <summary>
        /// Resolves and validates every persisted file belonging to one requested series.
        /// </summary>
        Task<ImportedSeriesLoadResult> ResolveAsync(ImportedSeriesLoadRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves multiple requested series atomically and requires all series to belong to the same study.
        /// </summary>
        Task<ImportedStudyLoadResult> ResolveAsync(IReadOnlyList<ImportedSeriesLoadRequest> requests, CancellationToken cancellationToken = default);
    }
}