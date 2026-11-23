namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.Imaging;

/// <summary>
/// Reports the result of a successful imaging-session save operation.
/// </summary>
public sealed record ImagingSessionSaveResult(int SnapshotChangeCount, int AcknowledgedChangeCount)
{
    /// <summary>
    /// Gets a value indicating whether the session still contains changes created during or after the saved snapshot.
    /// </summary>
    public bool HasRemainingChanges => AcknowledgedChangeCount < SnapshotChangeCount;
}