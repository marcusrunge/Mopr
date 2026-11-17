namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Imaging
{
    /// <summary>
    /// Reports the stable identities committed by one atomic imaging-session save operation.
    /// </summary>
    public sealed record ImagingSessionPersistenceSaveResult(IReadOnlyList<Guid> PersistedMeasurementEntityIds);
}
