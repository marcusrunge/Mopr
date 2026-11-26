using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    /// <summary>Determines whether two viewports share the identical ordered DICOM instance sequence.</summary>
    public interface IViewportSliceSynchronizationCompatibilityService
    {
        bool AreCompatible(ViewportSliceSynchronizationContext source, ViewportSliceSynchronizationContext target);
    }
}
