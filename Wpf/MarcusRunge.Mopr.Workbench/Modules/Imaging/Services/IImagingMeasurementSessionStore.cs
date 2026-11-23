using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using System.Collections.ObjectModel;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    /// <summary>
    /// Owns the authoritative in-memory measurement collections of the currently open study.
    /// </summary>
    public interface IImagingMeasurementSessionStore
    {
        /// <summary>
        /// Removes all in-memory measurement state without creating persistence changes.
        /// </summary>
        void Clear();

        /// <summary>
        /// Adds a completed new measurement and registers it as created.
        /// </summary>
        void Create(ImagingMeasurementInstanceKey instanceKey, ViewportMeasurementViewModel measurement);

        /// <summary>
        /// Deletes one measurement and registers the corresponding persistence change.
        /// </summary>
        bool Delete(ImagingMeasurementInstanceKey instanceKey, ViewportMeasurementViewModel measurement);

        /// <summary>
        /// Gets the stable shared collection for one DICOM instance.
        /// </summary>
        ObservableCollection<ViewportMeasurementViewModel> GetMeasurements(ImagingMeasurementInstanceKey instanceKey);

        /// <summary>
        /// Adds a persisted measurement without registering an unsaved change.
        /// </summary>
        void Load(ImagingMeasurementInstanceKey instanceKey, ViewportMeasurementViewModel measurement);
    }
}