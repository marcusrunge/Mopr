namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies
{
    /// <summary>
    /// Identifies one imported DICOM series requested for safe repository loading.
    /// </summary>
    public sealed record ImportedSeriesLoadRequest(int SeriesId, string SeriesInstanceUid);
}