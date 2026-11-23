namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Models.Imaging
{
    /// <summary>
    /// Describes one atomic imaging-session persistence operation.
    /// </summary>
    public sealed record ImagingSessionPersistenceSaveRequest
    {
        /// <summary>
        /// Initializes a validated imaging-session persistence request.
        /// </summary>
        public ImagingSessionPersistenceSaveRequest(string studyInstanceUid, int auditUserId, IReadOnlyList<ImagingMeasurementPersistenceChange> measurementChanges)
        {
            if (string.IsNullOrWhiteSpace(studyInstanceUid)) throw new ArgumentException("The study instance UID must not be empty.", nameof(studyInstanceUid));
            if (auditUserId <= 0) throw new ArgumentOutOfRangeException(nameof(auditUserId), "The audit user ID must be positive.");

            StudyInstanceUid = studyInstanceUid.Trim();
            AuditUserId = auditUserId;
            MeasurementChanges = measurementChanges ?? throw new ArgumentNullException(nameof(measurementChanges));
        }

        public string StudyInstanceUid { get; }
        public int AuditUserId { get; }
        public IReadOnlyList<ImagingMeasurementPersistenceChange> MeasurementChanges { get; }
    }
}