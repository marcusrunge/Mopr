using MarcusRunge.Mopr.Workbench.Modules.Imaging.Enums;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class ViewportSynchronizationSettingsViewModelTests
    {
        [Fact]
        public void CreateGroupCommand_Should_Create_Group_With_Active_Viewport_And_No_Flags()
        {
            var context = new TestContext("Viewport1");
            context.ViewModel.NewGroupName = "Comparison";

            context.ViewModel.CreateGroupCommand.Execute();

            var group = Assert.Single(context.Service.GetGroups());
            Assert.Equal("Comparison", group.Name);
            Assert.Equal(["Viewport1"], group.ViewportIds);
            Assert.Equal(ViewportSynchronizationFlags.None, group.Flags);
            Assert.Equal(group.Id, context.ViewModel.SelectedGroup?.Id);
        }

        [Fact]
        public void AddActiveViewportCommand_Should_Enable_Routes_Between_Two_Viewports()
        {
            var context = new TestContext("Viewport1");
            context.ViewModel.NewGroupName = "Comparison";
            context.ViewModel.CreateGroupCommand.Execute();
            context.SelectViewport("Viewport2");

            context.ViewModel.AddActiveViewportCommand.Execute();
            context.ViewModel.IsZoomEnabled = true;

            var route = Assert.Single(context.Service.GetRoutes("Viewport1", ViewportSynchronizationFlags.Zoom));
            Assert.Equal("Viewport2", route.TargetViewportId);
        }

        [Fact]
        public void RemoveActiveViewportCommand_Should_Remove_All_Routes_To_Viewport()
        {
            var context = new TestContext("Viewport1");
            context.ViewModel.NewGroupName = "Comparison";
            context.ViewModel.CreateGroupCommand.Execute();
            context.SelectViewport("Viewport2");
            context.ViewModel.AddActiveViewportCommand.Execute();
            context.ViewModel.IsPanEnabled = true;

            context.ViewModel.RemoveActiveViewportCommand.Execute();

            Assert.Empty(context.Service.GetRoutes("Viewport1", ViewportSynchronizationFlags.Pan));
        }

        [Fact]
        public void RemoveGroupCommand_Should_Remove_Group_And_Routes()
        {
            var context = new TestContext("Viewport1");
            context.ViewModel.NewGroupName = "Comparison";
            context.ViewModel.CreateGroupCommand.Execute();
            context.SelectViewport("Viewport2");
            context.ViewModel.AddActiveViewportCommand.Execute();
            context.ViewModel.IsRotationEnabled = true;

            context.ViewModel.RemoveGroupCommand.Execute();

            Assert.Empty(context.Service.GetGroups());
            Assert.Empty(context.Service.GetRoutes("Viewport1", ViewportSynchronizationFlags.Rotation));
        }

        private sealed class TestContext
        {
            private string _activeViewportId;

            public TestContext(string activeViewportId)
            {
                _activeViewportId = activeViewportId;
                SelectionService.SetupGet(service => service.ActiveViewportId).Returns(() => _activeViewportId);
                ImagingService.SetupGet(service => service.ImagingViewportSelectionService).Returns(SelectionService.Object);
                Core.SetupGet(service => service.ImagingService).Returns(ImagingService.Object);
                ViewModel = new ViewportSynchronizationSettingsViewModel(Core.Object, Service);
            }

            public Mock<ICore> Core { get; } = new();
            public Mock<IImagingService> ImagingService { get; } = new();
            public Mock<IImagingViewportSelectionService> SelectionService { get; } = new();
            public ViewportSynchronizationService Service { get; } = new();
            public ViewportSynchronizationSettingsViewModel ViewModel { get; }

            public void SelectViewport(string viewportId)
            {
                var previousViewportId = _activeViewportId;
                _activeViewportId = viewportId;
                SelectionService.Raise(service => service.ActiveViewportChanged += null, new ImagingViewportSelectionChangedEventArgs(previousViewportId, viewportId));
            }
        }
    }
}
