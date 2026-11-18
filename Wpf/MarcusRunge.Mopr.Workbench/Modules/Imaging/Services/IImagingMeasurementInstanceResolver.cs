using MarcusRunge.Mopr.Workbench.Contracts.Models;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    /// <summary>
    /// Resolves the stable DICOM measurement owner for a file displayed from an imaging series.
    /// </summary>
    public interface IImagingMeasurementInstanceResolver
    {
        /// <summary>
        /// Resolves the Study, Series and SOP Instance UIDs of the displayed file.
        /// </summary>
        ImagingMeasurementInstanceKey Resolve(SeriesInfo series, string filePath);
    }
}
