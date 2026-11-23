using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Imaging
{
    /// <summary>
    /// Describes one measurement change to persist as part of an imaging-session save operation.
    /// </summary>
    public sealed record ImagingMeasurementPersistenceChange
    {
        /// <summary>
        /// Initializes one validated measurement persistence change.
        /// </summary>
        public ImagingMeasurementPersistenceChange(Guid entityId, ImagingSessionChangeState state, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, MeasurementType measurementType, MeasurementData? data, string? title, string? description)
        {
            if (entityId == Guid.Empty) throw new ArgumentException("The measurement entity ID must not be empty.", nameof(entityId));
            if (string.IsNullOrWhiteSpace(studyInstanceUid)) throw new ArgumentException("The study instance UID must not be empty.", nameof(studyInstanceUid));
            if (string.IsNullOrWhiteSpace(seriesInstanceUid)) throw new ArgumentException("The series instance UID must not be empty.", nameof(seriesInstanceUid));
            if (string.IsNullOrWhiteSpace(sopInstanceUid)) throw new ArgumentException("The SOP instance UID must not be empty.", nameof(sopInstanceUid));
            if (state != ImagingSessionChangeState.Deleted && data is null) throw new ArgumentNullException(nameof(data), "Created and modified measurements require measurement data.");
            if (state != ImagingSessionChangeState.Deleted && measurementType == MeasurementType.Unknown) throw new ArgumentException("Created and modified measurements require a supported measurement type.", nameof(measurementType));

            EntityId = entityId;
            State = state;
            StudyInstanceUid = studyInstanceUid.Trim();
            SeriesInstanceUid = seriesInstanceUid.Trim();
            SopInstanceUid = sopInstanceUid.Trim();
            MeasurementType = measurementType;
            Data = data;
            Title = title;
            Description = description;
        }

        public Guid EntityId { get; }
        public ImagingSessionChangeState State { get; }
        public string StudyInstanceUid { get; }
        public string SeriesInstanceUid { get; }
        public string SopInstanceUid { get; }
        public MeasurementType MeasurementType { get; }
        public MeasurementData? Data { get; }
        public string? Title { get; }
        public string? Description { get; }
    }
}