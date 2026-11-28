using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Models
{
    /// <summary>
    /// Provides the immutable ordered DICOM frame identity required to validate slice synchronization.
    /// </summary>
    public sealed class ViewportSliceSynchronizationContext
    {
        public ViewportSliceSynchronizationContext(string viewportId, string seriesId, IReadOnlyList<DicomImagePosition> orderedImagePositions, int currentSlice, int sliceCount)
        {
            if (string.IsNullOrWhiteSpace(viewportId)) throw new ArgumentException("The viewport ID must not be empty.", nameof(viewportId));
            if (string.IsNullOrWhiteSpace(seriesId)) throw new ArgumentException("The series ID must not be empty.", nameof(seriesId));
            ArgumentNullException.ThrowIfNull(orderedImagePositions);
            if (orderedImagePositions.Count == 0) throw new ArgumentException("At least one ordered DICOM image position is required.", nameof(orderedImagePositions));
            if (orderedImagePositions.Any(position => position is null)) throw new ArgumentException("Ordered image positions must not contain null entries.", nameof(orderedImagePositions));
            if (sliceCount != orderedImagePositions.Count) throw new ArgumentOutOfRangeException(nameof(sliceCount), sliceCount, "Slice count must match the ordered DICOM image-position count.");
            if (currentSlice < 1 || currentSlice > sliceCount) throw new ArgumentOutOfRangeException(nameof(currentSlice), currentSlice, "Current slice must be within the available slice range.");

            ViewportId = viewportId;
            SeriesId = seriesId;
            OrderedImagePositions = Array.AsReadOnly(orderedImagePositions.ToArray());
            CurrentSlice = currentSlice;
            SliceCount = sliceCount;
        }

        public int CurrentSlice { get; }
        public IReadOnlyList<DicomImagePosition> OrderedImagePositions { get; }
        public string SeriesId { get; }
        public int SliceCount { get; }
        public string ViewportId { get; }
    }
}
