using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Models
{
    /// <summary>
    /// Identifies one decoded DICOM frame in the viewer cache without coupling cache identity to display order.
    /// </summary>
    internal readonly struct DicomFrameCacheKey : IEquatable<DicomFrameCacheKey>
    {
        internal DicomFrameCacheKey(string filePath, int frameIndex)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("The DICOM file path must not be empty.", nameof(filePath));
            if (frameIndex < 0) throw new ArgumentOutOfRangeException(nameof(frameIndex), frameIndex, "The frame index must not be negative.");

            FilePath = filePath;
            FrameIndex = frameIndex;
        }

        internal string FilePath { get; }
        internal int FrameIndex { get; }
        public bool Equals(DicomFrameCacheKey other) => FrameIndex == other.FrameIndex && string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object? obj) => obj is DicomFrameCacheKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(FilePath), FrameIndex);
        public static bool operator ==(DicomFrameCacheKey left, DicomFrameCacheKey right) => left.Equals(right);
        public static bool operator !=(DicomFrameCacheKey left, DicomFrameCacheKey right) => !left.Equals(right);
    }
}
