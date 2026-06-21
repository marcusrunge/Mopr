using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Bases
{
    internal class ImagingServiceBase : IImagingServiceBase, IImagingService
    {
        protected IImagingLayoutService? _imagingLayoutService;
        protected IImagingSelectionService? _imagingSelectionService;
        protected IImagingStudyService? _imagingStudyService;
        protected IImagingToolService? _imagingToolService;
        protected IImagingViewportSelectionService? _imagingViewportSelectionService;
        protected IImagingViewportService? _imagingViewportService;

        public ImagingServiceBase(ICoreBase? coreBase) => CoreBase = coreBase;

        public ICoreBase? CoreBase { get; private set; }

        public IImagingViewportSelectionService? IImagingViewportSelectionService => _imagingViewportSelectionService;
        public IImagingViewportService? IImagingViewportService => _imagingViewportService;
        public IImagingLayoutService? ImagingLayoutService => _imagingLayoutService;

        public IImagingSelectionService? ImagingSelectionService => _imagingSelectionService;

        public IImagingStudyService? ImagingStudyService => _imagingStudyService;

        public IImagingToolService? ImagingToolService => _imagingToolService;

        public IImagingViewportSelectionService? ImagingViewportSelectionService => _imagingViewportSelectionService;
    }
}