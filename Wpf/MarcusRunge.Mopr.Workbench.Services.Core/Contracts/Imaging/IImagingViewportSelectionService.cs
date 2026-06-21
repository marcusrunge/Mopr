namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public interface IImagingViewportSelectionService
    {
        event EventHandler<ImagingViewportSelectionChangedEventArgs>? ActiveViewportChanged;

        string ActiveViewportId { get; }

        void SelectViewport(string viewportId);

        void SetDefaultViewport(string viewportId);
    }
}