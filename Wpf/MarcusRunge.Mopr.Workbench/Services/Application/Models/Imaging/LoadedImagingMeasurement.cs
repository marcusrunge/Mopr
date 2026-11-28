using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.Imaging;

/// <summary>
/// Represents one persisted measurement prepared for hydration into the imaging session.
/// </summary>
public sealed record LoadedImagingMeasurement(Guid EntityId, string StudyInstanceUid, string SeriesInstanceUid, string SopInstanceUid, MeasurementType MeasurementType, MeasurementData Data, string? Title, string? Description, int CreatedByUserId, DateTime CreatedAtUtc, int? ModifiedByUserId, DateTime? ModifiedAtUtc, int FrameIndex = 0);
