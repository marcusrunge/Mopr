using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    /// <summary>
    /// Validates slice synchronization against complete ordered DICOM frame identity instead of relying on display indices or physical file count.
    /// </summary>
    public sealed class ViewportSliceSynchronizationCompatibilityService : IViewportSliceSynchronizationCompatibilityService
    {
        /// <inheritdoc/>
        public bool AreCompatible(ViewportSliceSynchronizationContext source, ViewportSliceSynchronizationContext target)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(target);
            if (string.Equals(source.ViewportId, target.ViewportId, StringComparison.Ordinal) || source.SliceCount != target.SliceCount) return false;

            for (var index = 0; index < source.OrderedImagePositions.Count; index++)
            {
                if (!HaveSameFrameIdentity(source.OrderedImagePositions[index], target.OrderedImagePositions[index])) return false;
            }

            return true;
        }

        private static bool HaveSameFrameIdentity(DicomImagePosition source, DicomImagePosition target) => !string.IsNullOrWhiteSpace(source.SopInstanceUid) && !string.IsNullOrWhiteSpace(target.SopInstanceUid) && source.FrameIndex == target.FrameIndex && string.Equals(source.SopInstanceUid, target.SopInstanceUid, StringComparison.Ordinal);
    }
}
