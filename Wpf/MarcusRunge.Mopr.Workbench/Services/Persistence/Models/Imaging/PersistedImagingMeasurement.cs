using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Imaging
{
    /// <summary>
    /// Represents one persisted measurement loaded for an imaging session.
    /// </summary>
    public sealed record PersistedImagingMeasurement(Guid EntityId, string StudyInstanceUid, string SeriesInstanceUid, string SopInstanceUid, MeasurementType MeasurementType, MeasurementData Data, string? Title, string? Description, int CreatedByUserId, DateTime CreatedAtUtc, int? ModifiedByUserId, DateTime? ModifiedAtUtc);
}