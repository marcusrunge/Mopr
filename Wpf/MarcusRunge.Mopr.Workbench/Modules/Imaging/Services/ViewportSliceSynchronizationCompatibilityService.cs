using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    /// <summary>Validates slice synchronization against complete DICOM identity instead of relying on display indices.</summary>
    public sealed class ViewportSliceSynchronizationCompatibilityService : IViewportSliceSynchronizationCompatibilityService
    {
        private readonly IImagingStudyService _imagingStudyService;

        public ViewportSliceSynchronizationCompatibilityService(ICore core)
        {
            ArgumentNullException.ThrowIfNull(core);
            _imagingStudyService = core.ImagingService?.ImagingStudyService ?? throw new InvalidOperationException("The imaging study service is not available through the core facade.");
        }

        /// <inheritdoc/>
        public bool AreCompatible(ViewportSliceSynchronizationContext source, ViewportSliceSynchronizationContext target)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(target);
            if (string.Equals(source.ViewportId, target.ViewportId, StringComparison.Ordinal) || source.SliceCount != target.SliceCount) return false;
            return TryCreateIdentity(source, out var sourceIdentity) && TryCreateIdentity(target, out var targetIdentity) && sourceIdentity.Equals(targetIdentity);
        }

        private bool TryCreateIdentity(ViewportSliceSynchronizationContext context, out SeriesIdentity identity)
        {
            var metadata = _imagingStudyService.GetDicomMetadataForSeries(context.SeriesId);
            if (metadata.Count != context.OrderedFilePaths.Count)
            {
                identity = default;
                return false;
            }

            Dictionary<string, DicomFileMetadata> metadataByPath = [with(StringComparer.OrdinalIgnoreCase)];
            foreach (var item in metadata)
            {
                if (string.IsNullOrWhiteSpace(item.FilePath) || !metadataByPath.TryAdd(item.FilePath, item))
                {
                    identity = default;
                    return false;
                }
            }

            List<string> sopInstanceUids = [with(context.OrderedFilePaths.Count)];
            string? studyInstanceUid = null, seriesInstanceUid = null;
            foreach (var filePath in context.OrderedFilePaths)
            {
                if (!metadataByPath.TryGetValue(filePath, out var item) || string.IsNullOrWhiteSpace(item.StudyInstanceUid) || string.IsNullOrWhiteSpace(item.SeriesInstanceUid) || string.IsNullOrWhiteSpace(item.SopInstanceUid))
                {
                    identity = default;
                    return false;
                }

                studyInstanceUid ??= item.StudyInstanceUid;
                seriesInstanceUid ??= item.SeriesInstanceUid;
                if (!string.Equals(studyInstanceUid, item.StudyInstanceUid, StringComparison.Ordinal) || !string.Equals(seriesInstanceUid, item.SeriesInstanceUid, StringComparison.Ordinal) || sopInstanceUids.Contains(item.SopInstanceUid, StringComparer.Ordinal))
                {
                    identity = default;
                    return false;
                }
                sopInstanceUids.Add(item.SopInstanceUid);
            }

            identity = new SeriesIdentity(studyInstanceUid!, seriesInstanceUid!, sopInstanceUids);
            return true;
        }

        private readonly record struct SeriesIdentity(string StudyInstanceUid, string SeriesInstanceUid, IReadOnlyList<string> SopInstanceUids)
        {
            public bool Equals(SeriesIdentity other) => string.Equals(StudyInstanceUid, other.StudyInstanceUid, StringComparison.Ordinal) && string.Equals(SeriesInstanceUid, other.SeriesInstanceUid, StringComparison.Ordinal) && SopInstanceUids.SequenceEqual(other.SopInstanceUids, StringComparer.Ordinal);
            public override int GetHashCode() => HashCode.Combine(StudyInstanceUid, SeriesInstanceUid, SopInstanceUids.Count);
        }
    }
}

