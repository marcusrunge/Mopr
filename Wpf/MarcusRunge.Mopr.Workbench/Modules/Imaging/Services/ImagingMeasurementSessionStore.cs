using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    /// <summary>
    /// Owns study-scoped measurement collections independently of viewport and slice lifetime.
    /// </summary>
    public sealed class ImagingMeasurementSessionStore(IImagingMeasurementChangeService changeService) : IImagingMeasurementSessionStore
    {
        private readonly IImagingMeasurementChangeService _changeService = changeService ?? throw new ArgumentNullException(nameof(changeService));
        private readonly Dictionary<ImagingMeasurementInstanceKey, ObservableCollection<ViewportMeasurementViewModel>> _measurements = [];
        private readonly Dictionary<Guid, MeasurementRegistration> _registrations = [];

        /// <inheritdoc/>
        public void Clear()
        {
            foreach (var registration in _registrations.Values) registration.Measurement.PropertyChanged -= OnMeasurementPropertyChanged;
            _registrations.Clear();
            _measurements.Clear();
        }

        /// <inheritdoc/>
        public void Create(ImagingMeasurementInstanceKey instanceKey, ViewportMeasurementViewModel measurement)
        {
            ValidateNewRegistration(instanceKey, measurement);
            GetMeasurements(instanceKey).Add(measurement);
            Register(instanceKey, measurement, measurement.ToChangeData());
            _changeService.MarkCreated(measurement, instanceKey.StudyInstanceUid, instanceKey.SeriesInstanceUid, instanceKey.SopInstanceUid);
        }

        /// <inheritdoc/>
        public bool Delete(ImagingMeasurementInstanceKey instanceKey, ViewportMeasurementViewModel measurement)
        {
            ArgumentNullException.ThrowIfNull(instanceKey);
            ArgumentNullException.ThrowIfNull(measurement);
            if (!_registrations.TryGetValue(measurement.Id, out var registration) || registration.InstanceKey != instanceKey) return false;

            registration.Measurement.PropertyChanged -= OnMeasurementPropertyChanged;
            _registrations.Remove(measurement.Id);
            bool removed = GetMeasurements(instanceKey).Remove(measurement);
            if (removed) _changeService.MarkDeleted(measurement, instanceKey.StudyInstanceUid, instanceKey.SeriesInstanceUid, instanceKey.SopInstanceUid);
            return removed;
        }

        /// <inheritdoc/>
        public ObservableCollection<ViewportMeasurementViewModel> GetMeasurements(ImagingMeasurementInstanceKey instanceKey)
        {
            ArgumentNullException.ThrowIfNull(instanceKey);
            if (_measurements.TryGetValue(instanceKey, out var measurements)) return measurements;

            measurements = [];
            _measurements.Add(instanceKey, measurements);
            return measurements;
        }

        /// <inheritdoc/>
        public void Load(ImagingMeasurementInstanceKey instanceKey, ViewportMeasurementViewModel measurement)
        {
            ValidateNewRegistration(instanceKey, measurement);
            GetMeasurements(instanceKey).Add(measurement);
            Register(instanceKey, measurement, measurement.ToChangeData());
        }

        private void OnMeasurementPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not ViewportMeasurementViewModel measurement || !IsPersistableProperty(e.PropertyName) || !_registrations.TryGetValue(measurement.Id, out var registration) || !measurement.IsComplete) return;

            var originalMeasurement = ViewportMeasurementViewModel.CreateLoaded(measurement.Id, ((LengthMeasurementData)registration.OriginalValue.Data).StartPoint.X, ((LengthMeasurementData)registration.OriginalValue.Data).StartPoint.Y, ((LengthMeasurementData)registration.OriginalValue.Data).EndPoint.X, ((LengthMeasurementData)registration.OriginalValue.Data).EndPoint.Y, measurement.PixelSpacingX, measurement.PixelSpacingY, registration.OriginalValue.Title, registration.OriginalValue.Description, measurement.CreatedAt, measurement.ModifiedAt);
            _changeService.MarkModified(measurement, originalMeasurement, registration.InstanceKey.StudyInstanceUid, registration.InstanceKey.SeriesInstanceUid, registration.InstanceKey.SopInstanceUid);
        }

        private void Register(ImagingMeasurementInstanceKey instanceKey, ViewportMeasurementViewModel measurement, ImagingMeasurementChangeData originalValue)
        {
            _registrations.Add(measurement.Id, new MeasurementRegistration(instanceKey, measurement, originalValue));
            measurement.PropertyChanged += OnMeasurementPropertyChanged;
        }

        private void ValidateNewRegistration(ImagingMeasurementInstanceKey instanceKey, ViewportMeasurementViewModel measurement)
        {
            ArgumentNullException.ThrowIfNull(instanceKey);
            ArgumentNullException.ThrowIfNull(measurement);
            if (!measurement.IsComplete) throw new InvalidOperationException($"Measurement '{measurement.Id}' is not complete.");
            if (_registrations.ContainsKey(measurement.Id)) throw new InvalidOperationException($"Measurement '{measurement.Id}' is already registered in the current imaging session.");
        }

        private static bool IsPersistableProperty(string? propertyName) => propertyName is nameof(ViewportMeasurementViewModel.Title) or nameof(ViewportMeasurementViewModel.Description) or nameof(ViewportMeasurementViewModel.EndImageX) or nameof(ViewportMeasurementViewModel.EndImageY);

        private sealed record MeasurementRegistration(ImagingMeasurementInstanceKey InstanceKey, ViewportMeasurementViewModel Measurement, ImagingMeasurementChangeData OriginalValue);
    }
}
