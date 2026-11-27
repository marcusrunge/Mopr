using System;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Models
{
    /// <summary>
    /// Identifies one renderable frame within a DICOM SOP instance.
    /// </summary>
    public sealed record DicomImagePosition
    {
        public DicomImagePosition(string filePath, string? sopInstanceUid, int frameIndex, int frameCount)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("The DICOM file path must not be empty.", nameof(filePath));
            if (frameCount <= 0) throw new ArgumentOutOfRangeException(nameof(frameCount), frameCount, "The frame count must be positive.");
            if (frameIndex < 0 || frameIndex >= frameCount) throw new ArgumentOutOfRangeException(nameof(frameIndex), frameIndex, $"The frame index must be between 0 and {frameCount - 1}.");

            FilePath = filePath;
            SopInstanceUid = string.IsNullOrWhiteSpace(sopInstanceUid) ? null : sopInstanceUid.Trim();
            FrameIndex = frameIndex;
            FrameCount = frameCount;
        }

        public string FilePath { get; }
        public int FrameCount { get; }
        public int FrameIndex { get; }
        public string? SopInstanceUid { get; }
    }
}