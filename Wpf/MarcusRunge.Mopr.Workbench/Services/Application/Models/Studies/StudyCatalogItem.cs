namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies
{
    /// <summary>
    /// Represents one imported DICOM study and its series in the application catalog.
    /// </summary>
    public sealed record StudyCatalogItem(int StudyId, string StudyInstanceUid, string? PatientName, DateTime? PatientBirthDate, string? PatientId, DateTime? StudyDate, string? Description, string? AccessionNumber, IReadOnlyList<SeriesCatalogItem> Series)
    {
        /// <summary>
        /// Gets the total number of persisted images in the study.
        /// </summary>
        public int ImageCount => Series.Sum(series => series.ImageCount);

        /// <summary>
        /// Gets the number of series in the study.
        /// </summary>
        public int SeriesCount => Series.Count;
    }
}