using MarcusRunge.Mopr.Workbench.Modules.Imaging.Enums;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class ViewportSynchronizationContextMenuTests
    {
        [Fact]
        public void AddViewportToGroupCommand_Should_Add_Explicit_Context_Viewport_And_Enable_Route()
        {
            var service = new ViewportSynchronizationService();
            var group = service.CreateGroup("Comparison", ["TwoByTwo.Viewport1"], ViewportSynchronizationFlags.Zoom);
            var imagingService = new Mock<IImagingService>();
            var selectionService = new Mock<IImagingViewportSelectionService>();
            var core = new Mock<ICore>();
            selectionService.SetupGet(value => value.ActiveViewportId).Returns("TwoByTwo.Viewport1");
            imagingService.SetupGet(value => value.ImagingViewportSelectionService).Returns(selectionService.Object);
            core.SetupGet(value => value.ImagingService).Returns(imagingService.Object);
            var viewModel = new ViewportSynchronizationSettingsViewModel(core.Object, service);
            var request = new ViewportSynchronizationGroupAssignmentRequest("TwoByTwo.Viewport2", group.Id);

            viewModel.AddViewportToGroupCommand.Execute(request);

            Assert.Contains("TwoByTwo.Viewport2", service.GetGroup(group.Id)!.ViewportIds);
            Assert.Equal("TwoByTwo.Viewport2", Assert.Single(service.GetRoutes("TwoByTwo.Viewport1", ViewportSynchronizationFlags.Zoom)).TargetViewportId);
        }

        [Fact]
        public void AddViewportToGroupCommand_Should_Not_Add_Duplicate_Membership()
        {
            var service = new ViewportSynchronizationService();
            var group = service.CreateGroup("Comparison", ["TwoByTwo.Viewport1", "TwoByTwo.Viewport2"]);
            var imagingService = new Mock<IImagingService>();
            var selectionService = new Mock<IImagingViewportSelectionService>();
            var core = new Mock<ICore>();
            imagingService.SetupGet(value => value.ImagingViewportSelectionService).Returns(selectionService.Object);
            core.SetupGet(value => value.ImagingService).Returns(imagingService.Object);
            var viewModel = new ViewportSynchronizationSettingsViewModel(core.Object, service);
            var request = new ViewportSynchronizationGroupAssignmentRequest("TwoByTwo.Viewport2", group.Id);

            Assert.False(viewModel.AddViewportToGroupCommand.CanExecute(request));
            Assert.Equal(2, service.GetGroup(group.Id)!.ViewportIds.Count);
        }
    }
}
