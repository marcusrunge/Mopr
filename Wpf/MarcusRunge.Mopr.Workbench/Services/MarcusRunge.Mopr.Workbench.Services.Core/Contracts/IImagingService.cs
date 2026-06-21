using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts
{
    public interface IImagingService
    {
        IImagingLayoutService? ImagingLayoutService { get; }
        IImagingSelectionService? ImagingSelectionService { get; }
        IImagingStudyService? ImagingStudyService { get; }
        IImagingToolService? ImagingToolService { get; }
        IImagingViewportSelectionService? ImagingViewportSelectionService { get; }
        IImagingViewportSelectionService? IImagingViewportSelectionService { get; }
        IImagingViewportService? ImagingViewportService { get; }
    }
}