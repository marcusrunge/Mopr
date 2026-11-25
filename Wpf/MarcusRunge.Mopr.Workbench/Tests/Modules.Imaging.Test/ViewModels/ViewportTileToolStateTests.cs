using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class ViewportTileToolStateTests
    {
        [Fact]
        public void SetActiveTool_Should_Expose_Pan_State()
        {
            var tile = CreateTile();

            tile.SetActiveTool(ImagingTool.Pan);

            Assert.Equal(ImagingTool.Pan, tile.ActiveTool);
            Assert.True(tile.IsPanActive);
            Assert.False(tile.IsZoomActive);
        }

        [Fact]
        public void SetActiveTool_Should_Expose_Zoom_State()
        {
            var tile = CreateTile();

            tile.SetActiveTool(ImagingTool.Zoom);

            Assert.Equal(ImagingTool.Zoom, tile.ActiveTool);
            Assert.False(tile.IsPanActive);
            Assert.True(tile.IsZoomActive);
        }

        [Theory]
        [InlineData(ImagingTool.None)]
        [InlineData(ImagingTool.WindowLevel)]
        [InlineData(ImagingTool.Crosshair)]
        [InlineData(ImagingTool.Measure)]
        public void SetActiveTool_Should_Disable_Pan_And_Zoom_For_Other_Tools(ImagingTool activeTool)
        {
            var tile = CreateTile();

            tile.SetActiveTool(activeTool);

            Assert.Equal(activeTool, tile.ActiveTool);
            Assert.False(tile.IsPanActive);
            Assert.False(tile.IsZoomActive);
        }

        [Fact]
        public void SetActiveTool_Should_Raise_Derived_Property_Notifications()
        {
            var tile = CreateTile();
            var changedProperties = new List<string?>();
            tile.PropertyChanged += (_, eventArgs) => changedProperties.Add(eventArgs.PropertyName);

            tile.SetActiveTool(ImagingTool.Pan);

            Assert.Contains(nameof(ViewportTileViewModel.ActiveTool), changedProperties);
            Assert.Contains(nameof(ViewportTileViewModel.IsPanActive), changedProperties);
            Assert.Contains(nameof(ViewportTileViewModel.IsZoomActive), changedProperties);
        }

        [Fact]
        public void Tool_Selection_Should_Not_Change_Viewport_Transform()
        {
            var tile = CreateTile();
            tile.SetZoomAndPan(3.0, 120.0, -80.0);

            tile.SetActiveTool(ImagingTool.Pan);
            tile.SetActiveTool(ImagingTool.Zoom);

            Assert.Equal(3.0, tile.ZoomFactor);
            Assert.Equal(120.0, tile.PanOffsetX);
            Assert.Equal(-80.0, tile.PanOffsetY);
        }

        private static ViewportTileViewModel CreateTile() => new(
            "viewport-1",
            "Viewport 1",
            Mock.Of<IImagingMeasurementSessionStore>(),
            Mock.Of<IImagingMeasurementInstanceResolver>());
    }
}
