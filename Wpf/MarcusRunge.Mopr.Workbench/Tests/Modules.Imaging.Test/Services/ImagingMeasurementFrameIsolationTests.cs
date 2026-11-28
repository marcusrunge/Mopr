using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.Services
{
    public sealed class ImagingMeasurementFrameIsolationTests
    {
        [Fact]
        public void SessionStore_Should_Keep_Measurements_Of_One_Sop_Instance_Separated_By_Frame()
        {
            var changeService = new Mock<IImagingMeasurementChangeService>(MockBehavior.Strict);
            var store = new ImagingMeasurementSessionStore(changeService.Object);
            var frameZeroKey = new ImagingMeasurementInstanceKey("study", "series", "instance", 0);
            var frameOneKey = new ImagingMeasurementInstanceKey("study", "series", "instance", 1);
            var frameZeroMeasurement = CreateMeasurement(1, 2, 3, 4);
            var frameOneMeasurement = CreateMeasurement(5, 6, 7, 8);

            store.Load(frameZeroKey, frameZeroMeasurement);
            store.Load(frameOneKey, frameOneMeasurement);

            Assert.Same(frameZeroMeasurement, Assert.Single(store.GetMeasurements(frameZeroKey)));
            Assert.Same(frameOneMeasurement, Assert.Single(store.GetMeasurements(frameOneKey)));
            Assert.NotSame(store.GetMeasurements(frameZeroKey), store.GetMeasurements(frameOneKey));
        }

        [Fact]
        public void InstanceKey_Should_Reject_Negative_Frame_Index() => Assert.Throws<ArgumentOutOfRangeException>(() => new ImagingMeasurementInstanceKey("study", "series", "instance", -1));

        private static ViewportMeasurementViewModel CreateMeasurement(double startX, double startY, double endX, double endY)
        {
            var measurement = new ViewportMeasurementViewModel(startX, startY);
            measurement.Complete(endX, endY);
            return measurement;
        }
    }
}
