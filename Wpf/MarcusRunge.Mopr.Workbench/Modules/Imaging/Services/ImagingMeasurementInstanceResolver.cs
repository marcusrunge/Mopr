using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using System;
using System.Linq;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    /// <summary>
    /// Resolves measurement ownership from the normalized metadata already held by the imaging study service.
    /// </summary>
    public sealed class ImagingMeasurementInstanceResolver : IImagingMeasurementInstanceResolver
    {
        private readonly IImagingStudyService _studyService;

        /// <summary>
        /// Initializes the resolver from the Core imaging facade.
        /// </summary>
        public ImagingMeasurementInstanceResolver(ICore core)
        {
            ArgumentNullException.ThrowIfNull(core);
            _studyService = core.ImagingService?.ImagingStudyService ?? throw new InvalidOperationException("The imaging study service is not available.");
        }

        /// <inheritdoc/>
        public ImagingMeasurementInstanceKey Resolve(SeriesInfo series, string filePath) => Resolve(series, filePath, 0);

        /// <inheritdoc/>
        public ImagingMeasurementInstanceKey Resolve(SeriesInfo series, string filePath, int frameIndex)
        {
            ArgumentNullException.ThrowIfNull(series);
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("The displayed DICOM file path must not be empty.", nameof(filePath));
            if (frameIndex < 0) throw new ArgumentOutOfRangeException(nameof(frameIndex), frameIndex, "The frame index must not be negative.");

            DicomFileMetadata metadata = _studyService.GetDicomMetadataForSeries(series.Id).SingleOrDefault(item => string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException($"No DICOM metadata is registered for displayed file '{filePath}'.");
            string studyInstanceUid = metadata.StudyInstanceUid ?? throw new InvalidOperationException($"Displayed DICOM file '{filePath}' has no Study Instance UID.");
            string seriesInstanceUid = metadata.SeriesInstanceUid ?? throw new InvalidOperationException($"Displayed DICOM file '{filePath}' has no Series Instance UID.");
            string sopInstanceUid = metadata.SopInstanceUid ?? throw new InvalidOperationException($"Displayed DICOM file '{filePath}' has no SOP Instance UID.");

            // SeriesInfo.Id is the imaging-service lookup key. Folder imports may intentionally use a generated key instead of the DICOM Series Instance UID.
            // The metadata collection was already resolved through that lookup key, so the DICOM metadata remains authoritative for persisted measurement identity.
            if (!string.IsNullOrWhiteSpace(series.StudyId) && !string.Equals(series.StudyId, studyInstanceUid, StringComparison.Ordinal)) throw new InvalidOperationException($"Displayed DICOM file '{filePath}' belongs to study '{studyInstanceUid}' instead of assigned study '{series.StudyId}'.");

            return new ImagingMeasurementInstanceKey(studyInstanceUid, seriesInstanceUid, sopInstanceUid, frameIndex);
        }
    }
}
