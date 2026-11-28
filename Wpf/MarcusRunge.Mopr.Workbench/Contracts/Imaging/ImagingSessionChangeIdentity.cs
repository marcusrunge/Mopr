using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Imaging
{
    /// <summary>
    /// Identifies one persistable entity within an imaging study independently of its UI representation.
    /// </summary>
    public sealed record ImagingSessionChangeIdentity
    {
        /// <summary>
        /// Initializes a new imaging-session change identity.
        /// </summary>
        public ImagingSessionChangeIdentity(string entityType, Guid entityId, string studyInstanceUid, string? seriesInstanceUid = null, string? sopInstanceUid = null, int frameIndex = 0)
        {
            if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("The entity type must not be empty.", nameof(entityType));
            if (entityId == Guid.Empty) throw new ArgumentException("The entity ID must not be empty.", nameof(entityId));
            if (string.IsNullOrWhiteSpace(studyInstanceUid)) throw new ArgumentException("The study instance UID must not be empty.", nameof(studyInstanceUid));
            if (frameIndex < 0) throw new ArgumentOutOfRangeException(nameof(frameIndex), frameIndex, "The frame index must not be negative.");

            EntityType = entityType.Trim();
            EntityId = entityId;
            StudyInstanceUid = studyInstanceUid.Trim();
            SeriesInstanceUid = NormalizeOptionalUid(seriesInstanceUid);
            SopInstanceUid = NormalizeOptionalUid(sopInstanceUid);
            FrameIndex = frameIndex;
        }

        /// <summary>
        /// Gets the stable type discriminator of the persistable entity.
        /// </summary>
        public int FrameIndex { get; }

        public string EntityType { get; }

        /// <summary>
        /// Gets the stable application identity of the persistable entity.
        /// </summary>
        public Guid EntityId { get; }

        /// <summary>
        /// Gets the DICOM study instance UID to which the entity belongs.
        /// </summary>
        public string StudyInstanceUid { get; }

        /// <summary>
        /// Gets the optional DICOM series instance UID to which the entity belongs.
        /// </summary>
        public string? SeriesInstanceUid { get; }

        /// <summary>
        /// Gets the optional DICOM SOP instance UID to which the entity belongs.
        /// </summary>
        public string? SopInstanceUid { get; }

        private static string? NormalizeOptionalUid(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
