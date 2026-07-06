using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    public sealed class ImagingMeasurementContext : BindableBase, IImagingMeasurementContext
    {
        private readonly ObservableCollection<ViewportMeasurementViewModel> _emptyMeasurements = [];

        private ViewportTileViewModel? _activeViewport;

        public ObservableCollection<ViewportMeasurementViewModel> ActiveMeasurements => _activeViewport?.Measurements ?? _emptyMeasurements;

        public bool HasSelectedMeasurement => SelectedMeasurement != null;

        public ViewportMeasurementViewModel? SelectedMeasurement
        {
            get => _activeViewport?.SelectedMeasurement;
            set
            {
                if (_activeViewport == null)
                {
                    return;
                }

                _activeViewport.SelectMeasurement(value);

                RaiseMeasurementContextChanged();
            }
        }

        public void DeleteMeasurements(IEnumerable<ViewportMeasurementViewModel> measurements)
        {
            if (_activeViewport == null)
            {
                return;
            }

            _activeViewport.DeleteMeasurements(measurements);

            RaiseMeasurementContextChanged();
        }

        public void SetActiveViewport(ViewportTileViewModel? viewport)
        {
            if (ReferenceEquals(_activeViewport, viewport))
            {
                return;
            }

            DetachActiveViewport();

            _activeViewport = viewport;

            AttachActiveViewport();

            RaiseMeasurementContextChanged();
        }

        private void AttachActiveViewport()
        {
            if (_activeViewport == null)
            {
                return;
            }

            _activeViewport.PropertyChanged -= OnActiveViewportPropertyChanged;
            _activeViewport.PropertyChanged += OnActiveViewportPropertyChanged;

            _activeViewport.Measurements.CollectionChanged -= OnMeasurementsCollectionChanged;
            _activeViewport.Measurements.CollectionChanged += OnMeasurementsCollectionChanged;

            foreach (var measurement in _activeViewport.Measurements)
            {
                measurement.PropertyChanged -= OnMeasurementPropertyChanged;
                measurement.PropertyChanged += OnMeasurementPropertyChanged;
            }
        }

        private void DetachActiveViewport()
        {
            if (_activeViewport == null)
            {
                return;
            }

            _activeViewport.PropertyChanged -= OnActiveViewportPropertyChanged;
            _activeViewport.Measurements.CollectionChanged -= OnMeasurementsCollectionChanged;

            foreach (var measurement in _activeViewport.Measurements)
            {
                measurement.PropertyChanged -= OnMeasurementPropertyChanged;
            }
        }

        private void OnActiveViewportPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewportTileViewModel.SelectedMeasurement) || e.PropertyName == nameof(ViewportTileViewModel.Measurements) || e.PropertyName == nameof(ViewportTileViewModel.ActiveMeasurementDraft) || e.PropertyName == nameof(ViewportTileViewModel.MeasurementDisplayText))
            {
                RaiseMeasurementContextChanged();
            }
        }

        private void OnMeasurementPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewportMeasurementViewModel.DisplayTitle) || e.PropertyName == nameof(ViewportMeasurementViewModel.LabelText) || e.PropertyName == nameof(ViewportMeasurementViewModel.IsSelected) || e.PropertyName == nameof(ViewportMeasurementViewModel.Title) || e.PropertyName == nameof(ViewportMeasurementViewModel.Description))
            {
                RaiseMeasurementContextChanged();
            }
        }

        private void OnMeasurementsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (ViewportMeasurementViewModel measurement in e.OldItems)
                {
                    measurement.PropertyChanged -= OnMeasurementPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (ViewportMeasurementViewModel measurement in e.NewItems)
                {
                    measurement.PropertyChanged -= OnMeasurementPropertyChanged;
                    measurement.PropertyChanged += OnMeasurementPropertyChanged;
                }
            }

            RaiseMeasurementContextChanged();
        }

        private void RaiseMeasurementContextChanged()
        {
            RaisePropertyChanged(nameof(ActiveMeasurements));
            RaisePropertyChanged(nameof(SelectedMeasurement));
            RaisePropertyChanged(nameof(HasSelectedMeasurement));
        }
    }
}