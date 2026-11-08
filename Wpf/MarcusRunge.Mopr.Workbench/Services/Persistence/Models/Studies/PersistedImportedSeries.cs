namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Studies
{
    /// <summary>
    /// Represents one persisted DICOM series and the repository assignments of its instances.
    /// </summary>
    public sealed record PersistedImportedSeries(int SeriesId, string SeriesInstanceUid, int StudyId, string StudyInstanceUid, IReadOnlyList<PersistedImportedSeriesInstance> Instances);
}