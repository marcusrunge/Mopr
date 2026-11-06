namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies
{
    /// <summary>
    /// Represents one imported DICOM series in the application study catalog.
    /// </summary>
    public sealed record SeriesCatalogItem(int SeriesId, string SeriesInstanceUid, int? SeriesNumber, string? Description, string? Modality, int ImageCount, int AvailableImageCount)
    {
        /// <summary>
        /// Gets a value indicating whether every persisted image has a usable repository assignment.
        /// </summary>
        public bool IsAvailable => ImageCount > 0 && AvailableImageCount == ImageCount;
    }
}