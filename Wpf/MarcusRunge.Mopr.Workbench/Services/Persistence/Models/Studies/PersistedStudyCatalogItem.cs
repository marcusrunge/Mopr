namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Studies
{
    /// <summary>
    /// Represents one persisted DICOM study and its series in the read-only catalog.
    /// </summary>
    public sealed record PersistedStudyCatalogItem(int StudyId, string StudyInstanceUid, string? PatientName, DateTime? PatientBirthDate, string? PatientId, DateTime? StudyDate, string? Description, string? AccessionNumber, IReadOnlyList<PersistedSeriesCatalogItem> Series)
    {
        /// <summary>
        /// Gets the number of persisted images in the study.
        /// </summary>
        public int ImageCount => Series.Sum(series => series.ImageCount);

        /// <summary>
        /// Gets the number of series in the study.
        /// </summary>
        public int SeriesCount => Series.Count;
    }
}