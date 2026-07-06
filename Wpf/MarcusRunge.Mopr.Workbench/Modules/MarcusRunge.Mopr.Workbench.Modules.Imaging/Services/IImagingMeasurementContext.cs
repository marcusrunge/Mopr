using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    public interface IImagingMeasurementContext : INotifyPropertyChanged
    {
        ObservableCollection<ViewportMeasurementViewModel> ActiveMeasurements { get; }
        bool HasSelectedMeasurement { get; }
        ViewportMeasurementViewModel? SelectedMeasurement { get; set; }

        void DeleteMeasurements(IEnumerable<ViewportMeasurementViewModel> measurements);

        void SetActiveViewport(ViewportTileViewModel? viewport);
    }
}