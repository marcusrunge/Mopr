using MarcusRunge.Mopr.Workbench.Services.Core.Bases;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Implementations
{
    internal sealed class ImagingService : ImagingServiceBase
    {
        internal ImagingService(ICoreBase? coreBase) : base(coreBase)
        {
            _imagingLayoutService = Imaging.ImagingLayoutService.Create(this);
            _imagingSelectionService = Imaging.ImagingSelectionService.Create(this);
            _imagingStudyService = Imaging.ImagingStudyService.Create(this);
            _imagingToolService = Imaging.ImagingToolService.Create(this);
            _imagingViewportSelectionService = Imaging.ImagingViewportSelectionService.Create(this);
            _imagingViewportService = Imaging.ImagingViewportService.Create(this);
            _imagingWindowLevelService = Imaging.ImagingWindowLevelService.Create(this);
        }

        internal static IImagingService? Create(ICoreBase? coreBase) => coreBase is null ? null : new ImagingService(coreBase);
    }
}