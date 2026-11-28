using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Behaviors;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using Moq;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class MultiFrameIntegrationRegressionTests
    {
        [Fact]
        public void KeyboardNavigation_Should_Use_All_Frames_Of_One_Sop_Instance()
        {
            var context = CreateContext(frameCount: 12);
            context.Tile.SetSlice(6);

            Assert.True(ViewportSliceNavigation.TryNavigate(context.Tile, Key.Home));
            Assert.Equal(0, context.Tile.CurrentImagePosition!.FrameIndex);
            Assert.True(ViewportSliceNavigation.TryNavigate(context.Tile, Key.End));
            Assert.Equal(11, context.Tile.CurrentImagePosition!.FrameIndex);
            Assert.True(ViewportSliceNavigation.TryNavigate(context.Tile, Key.PageDown));
            Assert.Equal(2, context.Tile.CurrentSlice);
            Assert.Equal(1, context.Tile.CurrentImagePosition!.FrameIndex);
            Assert.True(ViewportSliceNavigation.TryNavigate(context.Tile, Key.PageUp));
            Assert.Equal(12, context.Tile.CurrentSlice);
            Assert.Equal(11, context.Tile.CurrentImagePosition!.FrameIndex);
        }

        [Fact]
        public void FrameChange_Should_Preserve_WindowLevel_And_ViewTransform()
        {
            var context = CreateContext(frameCount: 3);
            context.Tile.SetWindowLevel(45, 350);
            context.Tile.SetZoomAndPan(2.5, 120, -40);
            context.Tile.RotateRight();
            context.Tile.FlipHorizontal();
            context.Tile.FlipVertical();

            context.Tile.SetSlice(2);

            Assert.Equal(45, context.Tile.WindowCenter);
            Assert.Equal(350, context.Tile.WindowWidth);
            Assert.Equal(2.5, context.Tile.ZoomFactor);
            Assert.Equal(120, context.Tile.PanOffsetX);
            Assert.Equal(-40, context.Tile.PanOffsetY);
            Assert.Equal(ViewportRotation.Clockwise90, context.Tile.Rotation);
            Assert.True(context.Tile.IsFlippedHorizontally);
            Assert.True(context.Tile.IsFlippedVertically);
            Assert.Equal(1, context.Tile.CurrentImagePosition!.FrameIndex);
        }

        [Fact]
        public void FrameChange_Should_Rebind_Measurements_Without_Mixing_Frames()
        {
            var context = CreateContext(frameCount: 3);
            var frameZeroMeasurements = context.Store.GetMeasurements(context.Keys[0]);
            var frameOneMeasurements = context.Store.GetMeasurements(context.Keys[1]);
            var frameZeroMeasurement = CreateMeasurement(1, 2, 3, 4);
            var frameOneMeasurement = CreateMeasurement(5, 6, 7, 8);
            context.Store.Load(context.Keys[0], frameZeroMeasurement);
            context.Store.Load(context.Keys[1], frameOneMeasurement);

            Assert.Same(frameZeroMeasurements, context.Tile.Measurements);
            Assert.Same(frameZeroMeasurement, Assert.Single(context.Tile.Measurements));

            context.Tile.SetSlice(2);

            Assert.Same(frameOneMeasurements, context.Tile.Measurements);
            Assert.Same(frameOneMeasurement, Assert.Single(context.Tile.Measurements));

            context.Tile.SetSlice(1);

            Assert.Same(frameZeroMeasurements, context.Tile.Measurements);
            Assert.Same(frameZeroMeasurement, Assert.Single(context.Tile.Measurements));
        }

        [Fact]
        public void Four_Viewports_Should_Maintain_Independent_MultiFrame_Positions()
        {
            MultiFrameContext[] contexts = [CreateContext("Viewport-1", 4), CreateContext("Viewport-2", 4), CreateContext("Viewport-3", 4), CreateContext("Viewport-4", 4)];

            contexts[0].Tile.SetSlice(1);
            contexts[1].Tile.SetSlice(2);
            contexts[2].Tile.SetSlice(3);
            contexts[3].Tile.SetSlice(4);

            Assert.Equal([0, 1, 2, 3], contexts.Select(context => context.Tile.CurrentImagePosition!.FrameIndex).ToArray());
            Assert.All(contexts, context => Assert.Equal(4, context.Tile.SliceCount));
        }

        private static MultiFrameContext CreateContext(int frameCount) => CreateContext("Viewport", frameCount);

        private static MultiFrameContext CreateContext(string viewportId, int frameCount)
        {
            var changeService = new Mock<IImagingMeasurementChangeService>(MockBehavior.Strict);
            var store = new ImagingMeasurementSessionStore(changeService.Object);
            SeriesInfo series = new("Series", "US", "Multi-frame", string.Empty, 1, "Study");
            ImagingMeasurementInstanceKey[] keys = Enumerable.Range(0, frameCount).Select(frameIndex => new ImagingMeasurementInstanceKey("Study", "Series", "1.2.3", frameIndex)).ToArray();
            var resolver = new Mock<IImagingMeasurementInstanceResolver>(MockBehavior.Strict);
            resolver.Setup(service => service.Resolve(series, "multi-frame.dcm", It.IsAny<int>())).Returns((SeriesInfo _, string _, int frameIndex) => keys[frameIndex]);
            var tile = new ViewportTileViewModel(viewportId, viewportId, store, resolver.Object);
            tile.SliceNavigationRequested += (_, eventArgs) => tile.SetLocalSlice(eventArgs.Delta == int.MinValue ? 1 : eventArgs.Delta == int.MaxValue ? tile.SliceCount : tile.CurrentSlice + eventArgs.Delta);
            DicomImagePosition[] positions = Enumerable.Range(0, frameCount).Select(frameIndex => new DicomImagePosition("multi-frame.dcm", "1.2.3", frameIndex, frameCount)).ToArray();
            tile.SetImagePositions(series, positions);
            return new MultiFrameContext(tile, store, keys);
        }

        private static ViewportMeasurementViewModel CreateMeasurement(double startX, double startY, double endX, double endY)
        {
            var measurement = new ViewportMeasurementViewModel(startX, startY);
            measurement.Complete(endX, endY);
            return measurement;
        }

        private sealed record MultiFrameContext(ViewportTileViewModel Tile, ImagingMeasurementSessionStore Store, IReadOnlyList<ImagingMeasurementInstanceKey> Keys);
    }
}
