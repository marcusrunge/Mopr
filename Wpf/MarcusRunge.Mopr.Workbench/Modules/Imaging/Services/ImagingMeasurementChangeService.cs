using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    /// <summary>
    /// Maps completed measurement operations to the single authoritative imaging-session change tracker.
    /// </summary>
    public sealed class ImagingMeasurementChangeService : IImagingMeasurementChangeService
    {
        private const string MeasurementEntityType = "Measurement";
        private readonly IImagingSessionChangeTracker _changeTracker;

        /// <summary>
        /// Initializes a measurement change service from the Core facade.
        /// </summary>
        public ImagingMeasurementChangeService(ICore core)
        {
            ArgumentNullException.ThrowIfNull(core);
            _changeTracker = core.ImagingService?.ImagingSessionChangeTracker ?? throw new InvalidOperationException("The imaging session change tracker is not available.");
        }

        /// <inheritdoc/>
        public ViewportMeasurementViewModel CreateLoadedMeasurement(Guid entityId, double startImageX, double startImageY, double endImageX, double endImageY, double? pixelSpacingX, double? pixelSpacingY, string? title, string? description, DateTime createdAtUtc, DateTime? modifiedAtUtc)
        {
            return ViewportMeasurementViewModel.CreateLoaded(entityId, startImageX, startImageY, endImageX, endImageY, pixelSpacingX, pixelSpacingY, title, description, createdAtUtc, modifiedAtUtc);
        }

        /// <inheritdoc/>
        public void MarkCreated(ViewportMeasurementViewModel measurement, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid) => MarkCreated(measurement, studyInstanceUid, seriesInstanceUid, sopInstanceUid, 0);

        /// <inheritdoc/>
        public void MarkCreated(ViewportMeasurementViewModel measurement, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, int frameIndex)
        {
            ArgumentNullException.ThrowIfNull(measurement);
            EnsureComplete(measurement);
            ImagingSessionChangeIdentity identity = CreateIdentity(measurement.Id, studyInstanceUid, seriesInstanceUid, sopInstanceUid, frameIndex);
            _changeTracker.MarkChanged(identity, ImagingSessionChangeState.Created, originalValue: null, measurement.ToChangeData());
        }

        /// <inheritdoc/>
        public void MarkModified(ViewportMeasurementViewModel measurement, ViewportMeasurementViewModel originalMeasurement, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid) => MarkModified(measurement, originalMeasurement, studyInstanceUid, seriesInstanceUid, sopInstanceUid, 0);

        /// <inheritdoc/>
        public void MarkModified(ViewportMeasurementViewModel measurement, ViewportMeasurementViewModel originalMeasurement, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, int frameIndex)
        {
            ArgumentNullException.ThrowIfNull(measurement);
            ArgumentNullException.ThrowIfNull(originalMeasurement);
            if (measurement.Id != originalMeasurement.Id) throw new ArgumentException("The original and current measurement must have the same stable identity.", nameof(originalMeasurement));
            EnsureComplete(measurement);
            EnsureComplete(originalMeasurement);
            ImagingSessionChangeIdentity identity = CreateIdentity(measurement.Id, studyInstanceUid, seriesInstanceUid, sopInstanceUid, frameIndex);
            _changeTracker.MarkChanged(identity, ImagingSessionChangeState.Modified, originalMeasurement.ToChangeData(), measurement.ToChangeData());
        }

        /// <inheritdoc/>
        public void MarkDeleted(ViewportMeasurementViewModel measurement, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid) => MarkDeleted(measurement, studyInstanceUid, seriesInstanceUid, sopInstanceUid, 0);

        /// <inheritdoc/>
        public void MarkDeleted(ViewportMeasurementViewModel measurement, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, int frameIndex)
        {
            ArgumentNullException.ThrowIfNull(measurement);
            EnsureComplete(measurement);
            ImagingSessionChangeIdentity identity = CreateIdentity(measurement.Id, studyInstanceUid, seriesInstanceUid, sopInstanceUid, frameIndex);
            _changeTracker.MarkChanged(identity, ImagingSessionChangeState.Deleted, measurement.ToChangeData(), currentValue: null);
        }

        private static ImagingSessionChangeIdentity CreateIdentity(Guid entityId, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid, int frameIndex) => new(MeasurementEntityType, entityId, studyInstanceUid, seriesInstanceUid, sopInstanceUid, frameIndex);

        private static void EnsureComplete(ViewportMeasurementViewModel measurement)
        {
            if (!measurement.IsComplete) throw new InvalidOperationException($"Measurement '{measurement.Id}' is not complete and cannot be persisted.");
        }
    }
}
