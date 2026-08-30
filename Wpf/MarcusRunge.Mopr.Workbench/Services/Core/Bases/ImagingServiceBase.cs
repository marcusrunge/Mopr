using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Bases
{
    internal class ImagingServiceBase(ICoreBase? coreBase) : IImagingServiceBase, IImagingService
    {
        protected IImagingLayoutService? _imagingLayoutService;
        protected IImagingSelectionService? _imagingSelectionService;
        protected IImagingStudyService? _imagingStudyService;
        protected IImagingToolService? _imagingToolService;
        protected IImagingViewportSelectionService? _imagingViewportSelectionService;
        protected IImagingViewportService? _imagingViewportService;
        protected IImagingWindowLevelService? _imagingWindowLevelService;
        private readonly ICoreBase? _coreBase = coreBase;

        ICoreBase? IImagingServiceBase.CoreBase => _coreBase;
        public IImagingLayoutService? ImagingLayoutService => _imagingLayoutService;
        public IImagingSelectionService? ImagingSelectionService => _imagingSelectionService;
        public IImagingStudyService? ImagingStudyService => _imagingStudyService;
        public IImagingToolService? ImagingToolService => _imagingToolService;
        public IImagingViewportSelectionService? ImagingViewportSelectionService => _imagingViewportSelectionService;
        public IImagingViewportService? ImagingViewportService => _imagingViewportService;
        public IImagingWindowLevelService? ImagingWindowLevelService => _imagingWindowLevelService;
    }
}