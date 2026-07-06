using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using System.Collections.ObjectModel;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    public interface IImagingMeasurementContext
    {
        ObservableCollection<ViewportMeasurementViewModel> ActiveMeasurements { get; }

        bool HasActiveMeasurements { get; }
        bool HasSelectedMeasurement { get; }
        ViewportMeasurementViewModel? SelectedMeasurement { get; set; }

        void ClearActiveMeasurements();

        void DeleteSelectedMeasurement();

        void SetActiveViewport(ViewportTileViewModel? viewport);
    }
}