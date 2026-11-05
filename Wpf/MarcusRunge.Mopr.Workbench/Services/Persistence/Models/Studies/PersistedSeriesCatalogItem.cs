namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Studies
{
    /// <summary>
    /// Represents one persisted DICOM series in the read-only study catalog.
    /// </summary>
    public sealed record PersistedSeriesCatalogItem(int SeriesId, string SeriesInstanceUid, int? SeriesNumber, string? Description, string? Modality, int ImageCount, int AvailableImageCount)
    {
        /// <summary>
        /// Gets a value indicating whether every persisted image has a usable repository assignment.
        /// </summary>
        public bool IsAvailable => ImageCount > 0 && AvailableImageCount == ImageCount;
    }
}