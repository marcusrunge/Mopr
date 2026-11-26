using MarcusRunge.Mopr.Workbench.Modules.Imaging.Enums;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class ViewportSynchronizationIntegrationTests
    {
        [Fact]
        public void Routed_Pan_Should_Update_Target_Without_Feedback()
        {
            var service = new ViewportSynchronizationService();
            service.CreateGroup("Comparison", ["source", "target"], ViewportSynchronizationFlags.Pan);
            var source = CreateTile("source");
            var target = CreateTile("target");
            var targetEvents = 0;
            target.ViewportStateChanged += (_, _) => targetEvents++;
            source.ViewportStateChanged += (_, args) => ApplyRoutes(service, args.Change, target);

            source.SetPan(23.0, -7.0);

            Assert.Equal(23.0, target.PanOffsetX);
            Assert.Equal(-7.0, target.PanOffsetY);
            Assert.Equal(0, targetEvents);
        }

        [Fact]
        public void Disabled_Zoom_Should_Not_Update_Target()
        {
            var service = new ViewportSynchronizationService();
            service.CreateGroup("Comparison", ["source", "target"], ViewportSynchronizationFlags.Pan);
            var source = CreateTile("source");
            var target = CreateTile("target");
            source.ViewportStateChanged += (_, args) => ApplyRoutes(service, args.Change, target);

            source.ZoomIn();

            Assert.Equal(1.0, target.ZoomFactor);
        }

        private static void ApplyRoutes(IViewportSynchronizationService service, ViewportStateChange change, ViewportTileViewModel target)
        {
            foreach (var route in service.GetRoutes(change.SourceViewportId, change.SynchronizationType))
            {
                if (!string.Equals(route.TargetViewportId, target.ViewportId, StringComparison.Ordinal)) continue;
                target.ApplySynchronizedStateChange(ToSynchronization(change));
            }
        }

        private static ViewportStateChange ToSynchronization(ViewportStateChange change) => change switch
        {
            ViewportPanChange value => new ViewportPanChange(value.SourceViewportId, value.PanOffsetX, value.PanOffsetY, ViewportStateChangeOrigin.Synchronization),
            ViewportZoomChange value => new ViewportZoomChange(value.SourceViewportId, value.ZoomFactor, ViewportStateChangeOrigin.Synchronization),
            _ => throw new ArgumentOutOfRangeException(nameof(change))
        };

        private static ViewportTileViewModel CreateTile(string viewportId) => new(viewportId, viewportId, Mock.Of<IImagingMeasurementSessionStore>(), Mock.Of<IImagingMeasurementInstanceResolver>());
    }
}
