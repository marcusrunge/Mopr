using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    /// <summary>
    /// Maps imaging measurement view models to the authoritative session change tracker.
    /// </summary>
    public interface IImagingMeasurementChangeService
    {
        /// <summary>
        /// Creates a view model from one persisted measurement without registering an unsaved change.
        /// </summary>
        ViewportMeasurementViewModel CreateLoadedMeasurement(Guid entityId, double startImageX, double startImageY, double endImageX, double endImageY, double? pixelSpacingX, double? pixelSpacingY, string? title, string? description, DateTime createdAtUtc, DateTime? modifiedAtUtc);

        /// <summary>
        /// Registers a completed new measurement as created.
        /// </summary>
        void MarkCreated(ViewportMeasurementViewModel measurement, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid);

        /// <summary>
        /// Registers an edited persisted or previously created measurement as modified.
        /// </summary>
        void MarkModified(ViewportMeasurementViewModel measurement, ViewportMeasurementViewModel originalMeasurement, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid);

        /// <summary>
        /// Registers removal of a measurement under its stable identity.
        /// </summary>
        void MarkDeleted(ViewportMeasurementViewModel measurement, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid);
    }
}
