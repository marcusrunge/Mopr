using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class ViewportTileTransformTests
    {
        [Fact]
        public void Constructor_Should_Create_Independent_FitToView_State()
        {
            var firstTile = CreateTile("viewport-1");
            var secondTile = CreateTile("viewport-2");

            firstTile.SetZoomAndPan(3.0, 120.0, -80.0);

            Assert.Equal(3.0, firstTile.ZoomFactor);
            Assert.Equal(120.0, firstTile.PanOffsetX);
            Assert.Equal(-80.0, firstTile.PanOffsetY);
            Assert.Equal(1.0, secondTile.ZoomFactor);
            Assert.Equal(0.0, secondTile.PanOffsetX);
            Assert.Equal(0.0, secondTile.PanOffsetY);
        }

        [Fact]
        public void ZoomAndPan_Should_Forward_PropertyChanged_Notifications()
        {
            var tile = CreateTile("viewport-1");
            var changedProperties = new List<string?>();
            tile.PropertyChanged += (_, eventArgs) => changedProperties.Add(eventArgs.PropertyName);

            tile.SetZoomAndPan(2.0, 25.0, -30.0);

            Assert.Contains(nameof(ViewportTileViewModel.ZoomFactor), changedProperties);
            Assert.Contains(nameof(ViewportTileViewModel.PanOffsetX), changedProperties);
            Assert.Contains(nameof(ViewportTileViewModel.PanOffsetY), changedProperties);
            Assert.Contains(nameof(ViewportTileViewModel.CanZoomIn), changedProperties);
            Assert.Contains(nameof(ViewportTileViewModel.CanZoomOut), changedProperties);
        }

        [Fact]
        public void AddPan_Should_Move_Only_Target_Viewport()
        {
            var firstTile = CreateTile("viewport-1");
            var secondTile = CreateTile("viewport-2");
            firstTile.SetPan(10.0, 20.0);

            firstTile.AddPan(15.0, -5.0);

            Assert.Equal(25.0, firstTile.PanOffsetX);
            Assert.Equal(15.0, firstTile.PanOffsetY);
            Assert.Equal(0.0, secondTile.PanOffsetX);
            Assert.Equal(0.0, secondTile.PanOffsetY);
        }

        [Fact]
        public void ResetViewTransform_Should_Restore_FitToView_State()
        {
            var tile = CreateTile("viewport-1");
            tile.SetZoomAndPan(4.0, 250.0, -130.0);

            tile.ResetViewTransform();

            Assert.Equal(1.0, tile.ZoomFactor);
            Assert.Equal(0.0, tile.PanOffsetX);
            Assert.Equal(0.0, tile.PanOffsetY);
        }

        [Fact]
        public void SetSeries_Should_Reset_ViewTransform()
        {
            var tile = CreateTile("viewport-1");
            var series = new SeriesInfo("1.2.3", "CT", "Series", string.Empty, 1, "1.2");
            tile.SetZoomAndPan(3.0, 100.0, -50.0);

            tile.SetSeries(series, ["C:\\Dicom\\instance.dcm"]);

            Assert.Equal(1.0, tile.ZoomFactor);
            Assert.Equal(0.0, tile.PanOffsetX);
            Assert.Equal(0.0, tile.PanOffsetY);
        }

        [Fact]
        public void Rotation_And_Flips_Should_Be_Independent_Per_Viewport()
        {
            var firstTile = CreateTile("viewport-1");
            var secondTile = CreateTile("viewport-2");

            firstTile.RotateRight();
            firstTile.FlipHorizontal();

            Assert.Equal(ViewportRotation.Clockwise90, firstTile.Rotation);
            Assert.True(firstTile.IsFlippedHorizontally);
            Assert.Equal(ViewportRotation.None, secondTile.Rotation);
            Assert.False(secondTile.IsFlippedHorizontally);
        }

        [Fact]
        public void FitToView_Should_Reset_Zoom_And_Pan()
        {
            var tile = CreateTile("viewport-1");
            tile.SetZoomAndPan(2.5, 75.0, 35.0);

            tile.FitToView();

            Assert.Equal(1.0, tile.ZoomFactor);
            Assert.Equal(0.0, tile.PanOffsetX);
            Assert.Equal(0.0, tile.PanOffsetY);
        }

        private static ViewportTileViewModel CreateTile(string viewportId) => new(
            viewportId,
            viewportId,
            Mock.Of<IImagingMeasurementSessionStore>(),
            Mock.Of<IImagingMeasurementInstanceResolver>());
    }
}
