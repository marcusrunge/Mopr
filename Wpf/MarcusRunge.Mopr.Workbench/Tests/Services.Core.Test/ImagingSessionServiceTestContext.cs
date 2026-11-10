using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Core.Implementations.Imaging;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Test
{
    internal sealed class ImagingSessionServiceTestContext
    {
        public ImagingSessionServiceTestContext(bool hasOpenStudy = false, bool hasUnsavedChanges = false)
        {
            ImagingServiceBase.As<IImagingService>().SetupGet(value => value.ImagingSelectionService).Returns(SelectionService.Object);
            ImagingServiceBase.As<IImagingService>().SetupGet(value => value.ImagingSessionChangeTracker).Returns(ChangeTracker.Object);
            ImagingServiceBase.As<IImagingService>().SetupGet(value => value.ImagingStudyService).Returns(StudyService.Object);
            ImagingServiceBase.As<IImagingService>().SetupGet(value => value.ImagingToolService).Returns(ToolService.Object);
            ImagingServiceBase.As<IImagingService>().SetupGet(value => value.ImagingViewportService).Returns(ViewportService.Object);
            ImagingServiceBase.As<IImagingService>().SetupGet(value => value.ImagingWindowLevelService).Returns(WindowLevelService.Object);
            StudyService.SetupGet(value => value.CurrentStudy).Returns(hasOpenStudy ? new StudyInfo("study", "Study", "Description") : null);
            ChangeTracker.SetupGet(value => value.HasUnsavedChanges).Returns(hasUnsavedChanges);
            Service = ImagingSessionService.Create(ImagingServiceBase.Object, CreationLifetime.Scoped) ?? throw new InvalidOperationException("The imaging session service could not be created.");
        }

        public Mock<IImagingSessionChangeTracker> ChangeTracker { get; } = new();
        public Mock<IImagingServiceBase> ImagingServiceBase { get; } = new();
        public Mock<IImagingSelectionService> SelectionService { get; } = new();
        public IImagingSessionService Service { get; }
        public Mock<IImagingStudyService> StudyService { get; } = new();
        public Mock<IImagingToolService> ToolService { get; } = new();
        public Mock<IImagingViewportService> ViewportService { get; } = new();
        public Mock<IImagingWindowLevelService> WindowLevelService { get; } = new();
    }
}