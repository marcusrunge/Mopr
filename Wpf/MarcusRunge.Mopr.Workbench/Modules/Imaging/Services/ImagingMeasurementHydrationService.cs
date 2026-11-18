using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    /// <summary>
    /// Hydrates supported persisted measurements without creating unsaved session changes.
    /// </summary>
    public sealed class ImagingMeasurementHydrationService : IImagingMeasurementHydrationService
    {
        private readonly IImagingSessionApplicationService _applicationService;
        private readonly IImagingMeasurementChangeService _changeService;
        private readonly IImagingMeasurementSessionStore _sessionStore;

        /// <summary>
        /// Initializes the hydration service from existing application and imaging facades.
        /// </summary>
        public ImagingMeasurementHydrationService(IApplication application, IImagingMeasurementChangeService changeService, IImagingMeasurementSessionStore sessionStore)
        {
            ArgumentNullException.ThrowIfNull(application);
            _applicationService = application.ImagingSession ?? throw new InvalidOperationException("The imaging session application service is not available.");
            _changeService = changeService ?? throw new ArgumentNullException(nameof(changeService));
            _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        }

        /// <inheritdoc/>
        public async Task HydrateAsync(string studyInstanceUid, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(studyInstanceUid)) throw new ArgumentException("The study instance UID must not be empty.", nameof(studyInstanceUid));

            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<LoadedImagingMeasurement> persistedMeasurements = await _applicationService.LoadMeasurementsAsync(studyInstanceUid.Trim(), cancellationToken).ConfigureAwait(false);
            var hydratedMeasurements = new List<(ImagingMeasurementInstanceKey Key, ViewportMeasurementViewModel Measurement)>(persistedMeasurements.Count);

            foreach (LoadedImagingMeasurement persistedMeasurement in persistedMeasurements)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!string.Equals(persistedMeasurement.StudyInstanceUid, studyInstanceUid, StringComparison.Ordinal)) throw new InvalidDataException($"Persisted measurement '{persistedMeasurement.EntityId}' belongs to study '{persistedMeasurement.StudyInstanceUid}' instead of requested study '{studyInstanceUid}'.");
                hydratedMeasurements.Add((CreateKey(persistedMeasurement), CreateMeasurement(persistedMeasurement)));
            }

            // The existing session is replaced only after the complete persisted payload has been validated and mapped successfully.
            _sessionStore.Clear();
            foreach (var hydratedMeasurement in hydratedMeasurements) _sessionStore.Load(hydratedMeasurement.Key, hydratedMeasurement.Measurement);
        }

        private ViewportMeasurementViewModel CreateMeasurement(LoadedImagingMeasurement persistedMeasurement)
        {
            if (persistedMeasurement.MeasurementType != MeasurementType.Length || persistedMeasurement.Data is not LengthMeasurementData lengthData) throw new NotSupportedException($"Persisted measurement '{persistedMeasurement.EntityId}' has unsupported type '{persistedMeasurement.MeasurementType}'.");
            return _changeService.CreateLoadedMeasurement(persistedMeasurement.EntityId, lengthData.StartPoint.X, lengthData.StartPoint.Y, lengthData.EndPoint.X, lengthData.EndPoint.Y, pixelSpacingX: null, pixelSpacingY: null, persistedMeasurement.Title, persistedMeasurement.Description, persistedMeasurement.CreatedAtUtc, persistedMeasurement.ModifiedAtUtc);
        }

        private static ImagingMeasurementInstanceKey CreateKey(LoadedImagingMeasurement persistedMeasurement) => new(persistedMeasurement.StudyInstanceUid, persistedMeasurement.SeriesInstanceUid, persistedMeasurement.SopInstanceUid);
    }
}
