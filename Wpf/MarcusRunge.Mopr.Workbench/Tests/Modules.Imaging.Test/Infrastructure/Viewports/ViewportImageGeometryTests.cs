using MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports;
using System.Windows;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.Infrastructure.Viewports
{
    public sealed class ViewportImageGeometryTests
    {
        [Fact]
        public void Constructor_Should_Calculate_Effective_Scale_And_Displayed_Size()
        {
            var geometry = new ViewportImageGeometry(fitScale: 0.5, zoomFactor: 2.0, offsetX: 10.0, offsetY: 20.0, frameWidth: 512, frameHeight: 256);

            Assert.Equal(0.5, geometry.FitScale);
            Assert.Equal(2.0, geometry.ZoomFactor);
            Assert.Equal(1.0, geometry.Scale);
            Assert.Equal(512.0, geometry.DisplayedWidth);
            Assert.Equal(256.0, geometry.DisplayedHeight);
        }

        [Theory]
        [InlineData(0.0, 0.0, 10.0, 20.0)]
        [InlineData(100.0, 50.0, 210.0, 120.0)]
        [InlineData(511.0, 255.0, 1032.0, 530.0)]
        public void ImagePointToHostPoint_Should_Apply_Zoom_And_Pan(double imageX, double imageY, double expectedHostX, double expectedHostY)
        {
            var geometry = new ViewportImageGeometry(fitScale: 1.0, zoomFactor: 2.0, offsetX: 10.0, offsetY: 20.0, frameWidth: 512, frameHeight: 256);

            var hostPoint = geometry.ImagePointToHostPoint(imageX, imageY);

            Assert.Equal(expectedHostX, hostPoint.X);
            Assert.Equal(expectedHostY, hostPoint.Y);
        }

        [Theory]
        [InlineData(10.0, 20.0, 0.0, 0.0)]
        [InlineData(210.0, 120.0, 100.0, 50.0)]
        [InlineData(1032.0, 530.0, 511.0, 255.0)]
        public void HostPointToImagePoint_Should_Reverse_Zoom_And_Pan(double hostX, double hostY, double expectedImageX, double expectedImageY)
        {
            var geometry = new ViewportImageGeometry(fitScale: 1.0, zoomFactor: 2.0, offsetX: 10.0, offsetY: 20.0, frameWidth: 512, frameHeight: 256);

            var imagePoint = geometry.HostPointToImagePoint(new Point(hostX, hostY));

            Assert.Equal(expectedImageX, imagePoint.X);
            Assert.Equal(expectedImageY, imagePoint.Y);
        }

        [Fact]
        public void Forward_And_Reverse_Transformation_Should_Preserve_Image_Position()
        {
            var geometry = new ViewportImageGeometry(fitScale: 0.75, zoomFactor: 3.25, offsetX: -180.5, offsetY: 92.25, frameWidth: 512, frameHeight: 512);
            var originalImagePoint = new Point(173.75, 314.125);

            var hostPoint = geometry.ImagePointToHostPoint(originalImagePoint.X, originalImagePoint.Y);
            var restoredImagePoint = geometry.HostPointToImagePoint(hostPoint);

            Assert.Equal(originalImagePoint.X, restoredImagePoint.X, precision: 10);
            Assert.Equal(originalImagePoint.Y, restoredImagePoint.Y, precision: 10);
        }

        [Theory]
        [InlineData(10.0, 20.0, true)]
        [InlineData(1033.9, 531.9, true)]
        [InlineData(9.999, 20.0, false)]
        [InlineData(10.0, 19.999, false)]
        [InlineData(1034.0, 20.0, false)]
        [InlineData(10.0, 532.0, false)]
        public void ContainsHostPoint_Should_Use_Transformed_Image_Bounds(double hostX, double hostY, bool expectedResult)
        {
            var geometry = new ViewportImageGeometry(fitScale: 1.0, zoomFactor: 2.0, offsetX: 10.0, offsetY: 20.0, frameWidth: 512, frameHeight: 256);

            Assert.Equal(expectedResult, geometry.ContainsHostPoint(new Point(hostX, hostY)));
        }

        [Theory]
        [InlineData(-0.1, 0.0, false)]
        [InlineData(0.0, -0.1, false)]
        [InlineData(0.0, 0.0, true)]
        [InlineData(511.999, 255.999, true)]
        [InlineData(512.0, 0.0, false)]
        [InlineData(0.0, 256.0, false)]
        public void ContainsImagePoint_Should_Use_Dicom_Frame_Bounds(double imageX, double imageY, bool expectedResult)
        {
            var geometry = new ViewportImageGeometry(fitScale: 1.0, zoomFactor: 1.0, offsetX: 0.0, offsetY: 0.0, frameWidth: 512, frameHeight: 256);

            Assert.Equal(expectedResult, geometry.ContainsImagePoint(imageX, imageY));
        }

        [Theory]
        [InlineData(-10.0, -20.0, 0, 0)]
        [InlineData(0.0, 0.0, 0, 0)]
        [InlineData(10.99, 20.99, 10, 20)]
        [InlineData(511.99, 255.99, 511, 255)]
        [InlineData(700.0, 400.0, 511, 255)]
        public void PixelConversion_Should_Floor_And_Clamp_To_Valid_Frame_Bounds(double imageX, double imageY, int expectedPixelX, int expectedPixelY)
        {
            var geometry = new ViewportImageGeometry(fitScale: 1.0, zoomFactor: 1.0, offsetX: 0.0, offsetY: 0.0, frameWidth: 512, frameHeight: 256);

            Assert.Equal(expectedPixelX, geometry.ToPixelX(imageX));
            Assert.Equal(expectedPixelY, geometry.ToPixelY(imageY));
        }

        [Theory]
        [InlineData(double.NaN, 1.0)]
        [InlineData(double.NegativeInfinity, 1.0)]
        [InlineData(double.PositiveInfinity, 1.0)]
        [InlineData(-5.0, 0.1)]
        [InlineData(0.0, 0.1)]
        [InlineData(0.05, 0.1)]
        [InlineData(0.1, 0.1)]
        [InlineData(1.0, 1.0)]
        [InlineData(16.0, 16.0)]
        [InlineData(32.0, 16.0)]
        public void ClampZoomFactor_Should_Return_Supported_Value(double zoomFactor, double expectedResult)
        {
            Assert.Equal(expectedResult, ViewportImageGeometryCalculator.ClampZoomFactor(zoomFactor));
        }

        [Fact]
        public void ZoomAnchoredPanOffset_Should_Preserve_ImagePoint_Below_Anchor()
        {
            var currentGeometry = new ViewportImageGeometry(fitScale: 0.5, zoomFactor: 1.0, offsetX: 100.0, offsetY: 50.0, frameWidth: 400, frameHeight: 300);
            var anchorHostPoint = new Point(175.0, 125.0);
            var anchoredImagePoint = currentGeometry.HostPointToImagePoint(anchorHostPoint);

            var targetPanOffset = ViewportImageGeometryCalculator.CalculateZoomAnchoredPanOffset(currentGeometry, targetZoomFactor: 2.0, anchorHostPoint, hostWidth: 400.0, hostHeight: 300.0);
            var targetScale = currentGeometry.FitScale * 2.0;
            var targetOffsetX = ((400.0 - currentGeometry.FrameWidth * targetScale) / 2.0) + targetPanOffset.X;
            var targetOffsetY = ((300.0 - currentGeometry.FrameHeight * targetScale) / 2.0) + targetPanOffset.Y;
            var targetGeometry = new ViewportImageGeometry(currentGeometry.FitScale, zoomFactor: 2.0, targetOffsetX, targetOffsetY, currentGeometry.FrameWidth, currentGeometry.FrameHeight);
            var transformedAnchor = targetGeometry.ImagePointToHostPoint(anchoredImagePoint.X, anchoredImagePoint.Y);

            Assert.Equal(anchorHostPoint.X, transformedAnchor.X, precision: 10);
            Assert.Equal(anchorHostPoint.Y, transformedAnchor.Y, precision: 10);
        }

        [Fact]
        public void Independent_Geometries_Should_Not_Share_Zoom_Or_Pan_State()
        {
            var firstGeometry = new ViewportImageGeometry(fitScale: 0.5, zoomFactor: 2.0, offsetX: 100.0, offsetY: 50.0, frameWidth: 512, frameHeight: 512);
            var secondGeometry = new ViewportImageGeometry(fitScale: 0.5, zoomFactor: 1.0, offsetX: 0.0, offsetY: 0.0, frameWidth: 512, frameHeight: 512);
            var imagePoint = new Point(100.0, 100.0);

            var firstHostPoint = firstGeometry.ImagePointToHostPoint(imagePoint.X, imagePoint.Y);
            var secondHostPoint = secondGeometry.ImagePointToHostPoint(imagePoint.X, imagePoint.Y);

            Assert.NotEqual(firstHostPoint, secondHostPoint);
            Assert.Equal(imagePoint.X, firstGeometry.HostPointToImagePoint(firstHostPoint).X, precision: 10);
            Assert.Equal(imagePoint.Y, firstGeometry.HostPointToImagePoint(firstHostPoint).Y, precision: 10);
            Assert.Equal(imagePoint.X, secondGeometry.HostPointToImagePoint(secondHostPoint).X, precision: 10);
            Assert.Equal(imagePoint.Y, secondGeometry.HostPointToImagePoint(secondHostPoint).Y, precision: 10);
        }

        [Fact]
        public void Placement_Should_Expose_Canonical_Image_Bounds()
        {
            var geometry = new ViewportImageGeometry(fitScale: 0.5, zoomFactor: 2.0, offsetX: -50.0, offsetY: 75.0, frameWidth: 512, frameHeight: 256);

            var placement = geometry.Placement;

            Assert.True(placement.IsValid);
            Assert.Equal(512.0, placement.Width);
            Assert.Equal(256.0, placement.Height);
            Assert.Equal(512.0, placement.ImageWidth);
            Assert.Equal(256.0, placement.ImageHeight);
            Assert.Equal(-50.0, placement.OffsetX);
            Assert.Equal(75.0, placement.OffsetY);
        }

        [Theory]
        [InlineData(90, 40.0, 10.0, 60.0, 100.0)]
        [InlineData(180, 90.0, 40.0, 100.0, 60.0)]
        [InlineData(270, 20.0, 90.0, 60.0, 100.0)]
        public void Rotation_Should_Map_Source_Points_To_Expected_Display_Coordinates(int rotation, double expectedX, double expectedY, double expectedWidth, double expectedHeight)
        {
            var geometry = new ViewportImageGeometry(fitScale: 1.0, zoomFactor: 1.0, offsetX: 0.0, offsetY: 0.0, frameWidth: 100, frameHeight: 60, (ViewportRotation)rotation);

            var displayPoint = geometry.ImagePointToDisplayPoint(10.0, 20.0);

            Assert.Equal(expectedX, displayPoint.X);
            Assert.Equal(expectedY, displayPoint.Y);
            Assert.Equal(expectedWidth, geometry.DisplayFrameWidth);
            Assert.Equal(expectedHeight, geometry.DisplayFrameHeight);
        }

        [Theory]
        [InlineData(0, false, false)]
        [InlineData(90, false, false)]
        [InlineData(180, true, false)]
        [InlineData(270, false, true)]
        [InlineData(90, true, true)]
        public void Display_Coordinates_And_Render_Transform_Should_Preserve_Source_Pixels(int rotation, bool flipHorizontal, bool flipVertical)
        {
            var geometry = new ViewportImageGeometry(fitScale: 2.0, zoomFactor: 1.0, offsetX: 7.0, offsetY: 9.0, frameWidth: 100, frameHeight: 60, (ViewportRotation)rotation, flipHorizontal, flipVertical);
            var sourcePoint = new Point(12.25, 23.75);

            var hostPoint = geometry.ImagePointToHostPoint(sourcePoint.X, sourcePoint.Y);
            var restoredPoint = geometry.HostPointToImagePoint(hostPoint);
            var rasterPoint = geometry.Placement.ImageTransform.Transform(new Point(sourcePoint.X * geometry.Scale, sourcePoint.Y * geometry.Scale));

            Assert.Equal(sourcePoint.X, restoredPoint.X, precision: 10);
            Assert.Equal(sourcePoint.Y, restoredPoint.Y, precision: 10);
            Assert.Equal(12, geometry.ToPixelX(restoredPoint.X));
            Assert.Equal(23, geometry.ToPixelY(restoredPoint.Y));
            Assert.Equal(hostPoint.X, geometry.OffsetX + rasterPoint.X, precision: 10);
            Assert.Equal(hostPoint.Y, geometry.OffsetY + rasterPoint.Y, precision: 10);
        }

        [Fact]
        public void Horizontal_And_Vertical_Flips_Should_Use_Display_Axes()
        {
            var horizontal = new ViewportImageGeometry(1.0, 1.0, 0.0, 0.0, 100, 60, isFlippedHorizontally: true);
            var vertical = new ViewportImageGeometry(1.0, 1.0, 0.0, 0.0, 100, 60, isFlippedVertically: true);

            Assert.Equal(new Point(90.0, 20.0), horizontal.ImagePointToDisplayPoint(10.0, 20.0));
            Assert.Equal(new Point(10.0, 40.0), vertical.ImagePointToDisplayPoint(10.0, 20.0));
        }
    }
}