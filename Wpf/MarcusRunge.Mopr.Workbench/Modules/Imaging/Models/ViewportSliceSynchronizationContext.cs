using System;
using System.Collections.Generic;
using System.Linq;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Models
{
    /// <summary>Provides the immutable series identity required to validate slice synchronization.</summary>
    public sealed class ViewportSliceSynchronizationContext
    {
        public ViewportSliceSynchronizationContext(string viewportId, string seriesId, IReadOnlyList<string> orderedFilePaths, int currentSlice, int sliceCount)
        {
            if (string.IsNullOrWhiteSpace(viewportId)) throw new ArgumentException("The viewport ID must not be empty.", nameof(viewportId));
            if (string.IsNullOrWhiteSpace(seriesId)) throw new ArgumentException("The series ID must not be empty.", nameof(seriesId));
            ArgumentNullException.ThrowIfNull(orderedFilePaths);
            if (orderedFilePaths.Count == 0) throw new ArgumentException("At least one ordered DICOM file path is required.", nameof(orderedFilePaths));
            if (orderedFilePaths.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Ordered DICOM file paths must not contain empty values.", nameof(orderedFilePaths));
            if (orderedFilePaths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != orderedFilePaths.Count) throw new ArgumentException("Ordered DICOM file paths must be unique.", nameof(orderedFilePaths));
            if (sliceCount != orderedFilePaths.Count) throw new ArgumentOutOfRangeException(nameof(sliceCount), sliceCount, "Slice count must match the ordered DICOM file count.");
            if (currentSlice < 1 || currentSlice > sliceCount) throw new ArgumentOutOfRangeException(nameof(currentSlice), currentSlice, "Current slice must be within the available slice range.");
            ViewportId = viewportId;
            SeriesId = seriesId;
            OrderedFilePaths = Array.AsReadOnly(orderedFilePaths.ToArray());
            CurrentSlice = currentSlice;
            SliceCount = sliceCount;
        }

        public int CurrentSlice { get; }
        public IReadOnlyList<string> OrderedFilePaths { get; }
        public string SeriesId { get; }
        public int SliceCount { get; }
        public string ViewportId { get; }
    }
}
