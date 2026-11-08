namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Studies
{
    /// <summary>
    /// Represents one persisted DICOM instance required to resolve an imported series.
    /// </summary>
    public sealed record PersistedImportedSeriesInstance(int InstanceId, string SopInstanceUid, int? InstanceNumber, int RepositoryLocationId, bool RepositoryLocationIsEnabled, string? RepositoryRootPath, string? RelativeFilePath);
}