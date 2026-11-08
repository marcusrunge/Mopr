using MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Studies;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Provides the read-only Persistence projection required to resolve an imported DICOM series.
    /// </summary>
    public interface IImportedSeriesRepository
    {
        /// <summary>
        /// Gets a persisted series by its database ID and DICOM series instance UID.
        /// </summary>
        /// <param name="seriesId">The positive database ID of the series.</param>
        /// <param name="seriesInstanceUid">The DICOM series instance UID.</param>
        /// <param name="cancellationToken">Cancels the read operation.</param>
        /// <returns>The matching series projection, or <see langword="null"/> when no exact match exists.</returns>
        Task<PersistedImportedSeries?> GetAsync(int seriesId, string seriesInstanceUid, CancellationToken cancellationToken = default);
    }
}