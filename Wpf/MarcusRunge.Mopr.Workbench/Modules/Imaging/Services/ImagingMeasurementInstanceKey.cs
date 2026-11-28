using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    /// <summary>
    /// Identifies the DICOM instance that owns a set of imaging measurements.
    /// </summary>
    public sealed record ImagingMeasurementInstanceKey
    {
        /// <summary>
        /// Initializes one validated DICOM measurement owner key.
        /// </summary>
        public ImagingMeasurementInstanceKey(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, int frameIndex = 0)
        {
            if (string.IsNullOrWhiteSpace(studyInstanceUid)) throw new ArgumentException("The study instance UID must not be empty.", nameof(studyInstanceUid));
            if (string.IsNullOrWhiteSpace(seriesInstanceUid)) throw new ArgumentException("The series instance UID must not be empty.", nameof(seriesInstanceUid));
            if (string.IsNullOrWhiteSpace(sopInstanceUid)) throw new ArgumentException("The SOP instance UID must not be empty.", nameof(sopInstanceUid));
            if (frameIndex < 0) throw new ArgumentOutOfRangeException(nameof(frameIndex), frameIndex, "The frame index must not be negative.");

            StudyInstanceUid = studyInstanceUid.Trim();
            SeriesInstanceUid = seriesInstanceUid.Trim();
            SopInstanceUid = sopInstanceUid.Trim();
            FrameIndex = frameIndex;
        }

        public int FrameIndex { get; }
        public string StudyInstanceUid { get; }
        public string SeriesInstanceUid { get; }
        public string SopInstanceUid { get; }
    }
}
