using System;

namespace MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging
{
    public interface IImagingViewportSelectionService
    {
        event EventHandler<ImagingViewportSelectionChangedEventArgs>? ActiveViewportChanged;

        string ActiveViewportId { get; }

        void SelectViewport(string viewportId);

        void SetDefaultViewportForLayout(string viewportId);
    }
}