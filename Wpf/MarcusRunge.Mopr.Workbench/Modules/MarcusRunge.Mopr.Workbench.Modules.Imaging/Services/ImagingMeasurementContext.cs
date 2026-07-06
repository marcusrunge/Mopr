using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using Prism.Mvvm;
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

        public bool HasActiveMeasurements => ActiveMeasurements.Count > 0;

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

        public void ClearActiveMeasurements()
        {
            if (_activeViewport == null)
            {
                return;
            }

            _activeViewport.ClearMeasurements();

            RaiseMeasurementContextChanged();
        }

        public void DeleteSelectedMeasurement()
        {
            if (_activeViewport == null)
            {
                return;
            }

            _activeViewport.DeleteSelectedMeasurement();

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
        }

        private void DetachActiveViewport()
        {
            if (_activeViewport == null)
            {
                return;
            }

            _activeViewport.PropertyChanged -= OnActiveViewportPropertyChanged;
            _activeViewport.Measurements.CollectionChanged -= OnMeasurementsCollectionChanged;
        }

        private void OnActiveViewportPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewportTileViewModel.SelectedMeasurement) || e.PropertyName == nameof(ViewportTileViewModel.Measurements) || e.PropertyName == nameof(ViewportTileViewModel.MeasurementDisplayText))
            {
                RaiseMeasurementContextChanged();
            }
        }

        private void OnMeasurementsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RaiseMeasurementContextChanged();
        }

        private void RaiseMeasurementContextChanged()
        {
            RaisePropertyChanged(nameof(ActiveMeasurements));
            RaisePropertyChanged(nameof(SelectedMeasurement));
            RaisePropertyChanged(nameof(HasActiveMeasurements));
            RaisePropertyChanged(nameof(HasSelectedMeasurement));
        }
    }
}