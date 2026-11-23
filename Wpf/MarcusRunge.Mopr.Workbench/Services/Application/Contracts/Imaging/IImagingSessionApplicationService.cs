using MarcusRunge.Mopr.Workbench.Services.Application.Models.Imaging;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Imaging;

/// <summary>
/// Coordinates persistence operations for the currently open imaging session.
/// </summary>
public interface IImagingSessionApplicationService
{
    /// <summary>
    /// Loads persisted measurements for the specified DICOM study without modifying the session change tracker.
    /// </summary>
    Task<IReadOnlyList<LoadedImagingMeasurement>> LoadMeasurementsAsync(string studyInstanceUid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the current authoritative change snapshot and acknowledges only the exact versions committed successfully.
    /// </summary>
    Task<ImagingSessionSaveResult> SaveAsync(CancellationToken cancellationToken = default);
}