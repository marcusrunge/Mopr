using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Enums;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using Moq;
using System.ComponentModel;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class ViewportTileStateChangeTests
    {
        [Fact]
        public void SetPan_Should_Publish_One_Atomic_Local_Change()
        {
            var tile = CreateTile("viewport-1");
            ViewportStateChange? change = null;
            (double X, double Y)? firstObservedPair = null;
            tile.PropertyChanged += (_, args) => firstObservedPair ??= args.PropertyName is nameof(tile.PanOffsetX) or nameof(tile.PanOffsetY) ? (tile.PanOffsetX, tile.PanOffsetY) : null;
            tile.ViewportStateChanged += (_, args) => change = args.Change;

            tile.SetPan(25.0, -12.0);

            var pan = Assert.IsType<ViewportPanChange>(change);
            Assert.Equal(ViewportStateChangeOrigin.Local, pan.Origin);
            Assert.Equal("viewport-1", pan.SourceViewportId);
            Assert.Equal((25.0, -12.0), firstObservedPair);
        }

        [Fact]
        public void SetWindowLevel_Should_Commit_Pair_Before_PropertyChanged()
        {
            var tile = CreateTile("viewport-1");
            (double? Center, double? Width)? firstObservedPair = null;
            tile.PropertyChanged += (_, args) => firstObservedPair ??= args.PropertyName is nameof(tile.WindowCenter) or nameof(tile.WindowWidth) ? (tile.WindowCenter, tile.WindowWidth) : null;

            tile.SetWindowLevel(40.0, 400.0);

            Assert.Equal((40.0, 400.0), firstObservedPair);
        }

        [Fact]
        public void ApplySynchronizedStateChange_Should_Update_Without_Publishing()
        {
            var tile = CreateTile("target");
            var published = false;
            tile.ViewportStateChanged += (_, _) => published = true;

            tile.ApplySynchronizedStateChange(new ViewportZoomChange("source", 3.0, ViewportStateChangeOrigin.Synchronization));

            Assert.Equal(3.0, tile.ZoomFactor);
            Assert.False(published);
        }

        [Fact]
        public void ApplySynchronizedStateChange_Should_Reject_Local_Origin() => Assert.Throws<ArgumentException>(() => CreateTile("target").ApplySynchronizedStateChange(new ViewportZoomChange("source", 2.0, ViewportStateChangeOrigin.Local)));

        [Fact]
        public void ApplySynchronizedStateChange_Should_Reject_Same_Source_And_Target() => Assert.Throws<ArgumentException>(() => CreateTile("viewport-1").ApplySynchronizedStateChange(new ViewportZoomChange("viewport-1", 2.0, ViewportStateChangeOrigin.Synchronization)));

        [Fact]
        public void Repeating_Effective_Value_Should_Not_Publish()
        {
            var tile = CreateTile("viewport-1");
            var count = 0;
            tile.ViewportStateChanged += (_, _) => count++;
            tile.SetPan(2.0, 3.0);

            tile.SetPan(2.0, 3.0);

            Assert.Equal(1, count);
        }

        [Fact]
        public void ResetViewTransform_Should_Publish_Deterministic_Changed_State_Order()
        {
            var tile = CreateTile("viewport-1");
            tile.SetZoomAndPan(2.0, 3.0, 4.0);
            tile.RotateRight();
            tile.FlipHorizontal();
            tile.FlipVertical();
            var types = new List<ViewportSynchronizationFlags>();
            tile.ViewportStateChanged += (_, args) => types.Add(args.Change.SynchronizationType);

            tile.ResetViewTransform();

            Assert.Equal([ViewportSynchronizationFlags.Zoom, ViewportSynchronizationFlags.Pan, ViewportSynchronizationFlags.Rotation, ViewportSynchronizationFlags.HorizontalFlip, ViewportSynchronizationFlags.VerticalFlip], types);
        }

        [Fact]
        public void SetSeries_Should_Reset_Transform_Silently()
        {
            var tile = CreateTile("viewport-1");
            tile.SetZoomAndPan(2.0, 3.0, 4.0);
            var count = 0;
            tile.ViewportStateChanged += (_, _) => count++;

            tile.SetSeries(new SeriesInfo("series", "CT", "Series", string.Empty, 1, "study"), ["instance.dcm"]);

            Assert.Equal(0, count);
            Assert.Equal(1.0, tile.ZoomFactor);
            Assert.Equal(0.0, tile.PanOffsetX);
            Assert.Equal(0.0, tile.PanOffsetY);
        }

        private static ViewportTileViewModel CreateTile(string viewportId) => new(viewportId, viewportId, Mock.Of<IImagingMeasurementSessionStore>(), Mock.Of<IImagingMeasurementInstanceResolver>());
    }
}
