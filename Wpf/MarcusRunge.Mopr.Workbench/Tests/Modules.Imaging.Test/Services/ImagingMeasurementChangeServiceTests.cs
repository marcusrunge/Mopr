using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.Services
{
    public sealed class ImagingMeasurementChangeServiceTests
    {
        [Fact]
        public void MarkCreated_Should_Register_Typed_Length_Measurement()
        {
            var context = new TestContext();
            var measurement = CreateCompletedMeasurement();

            context.Service.MarkCreated(measurement, TestContext.StudyInstanceUid, TestContext.SeriesInstanceUid, TestContext.SopInstanceUid);

            context.ChangeTracker.Verify(tracker => tracker.MarkChanged(It.Is<ImagingSessionChangeIdentity>(identity => identity.EntityType == "Measurement" && identity.EntityId == measurement.Id && identity.StudyInstanceUid == TestContext.StudyInstanceUid && identity.SeriesInstanceUid == TestContext.SeriesInstanceUid && identity.SopInstanceUid == TestContext.SopInstanceUid), ImagingSessionChangeState.Created, null, It.Is<ImagingMeasurementChangeData>(data => data.MeasurementType == MeasurementType.Length && data.Title == "Length")), Times.Once);
        }

        [Fact]
        public void MarkDeleted_Should_Register_Original_Value_Without_Current_Value()
        {
            var context = new TestContext();
            var measurement = CreateCompletedMeasurement();

            context.Service.MarkDeleted(measurement, TestContext.StudyInstanceUid, TestContext.SeriesInstanceUid, TestContext.SopInstanceUid);

            context.ChangeTracker.Verify(tracker => tracker.MarkChanged(It.IsAny<ImagingSessionChangeIdentity>(), ImagingSessionChangeState.Deleted, It.Is<ImagingMeasurementChangeData>(data => data.MeasurementType == MeasurementType.Length), null), Times.Once);
        }

        [Fact]
        public void MarkModified_Should_Require_Stable_Identity()
        {
            var context = new TestContext();
            var original = CreateCompletedMeasurement();
            var current = CreateCompletedMeasurement();

            Assert.Throws<ArgumentException>(() => context.Service.MarkModified(current, original, TestContext.StudyInstanceUid, TestContext.SeriesInstanceUid, TestContext.SopInstanceUid));
        }

        [Fact]
        public void CreateLoadedMeasurement_Should_Not_Register_Change()
        {
            var context = new TestContext();
            var entityId = Guid.NewGuid();

            ViewportMeasurementViewModel measurement = context.Service.CreateLoadedMeasurement(entityId, 1, 2, 3, 4, 0.5, 0.5, "Loaded", "Persisted", DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);

            Assert.Equal(entityId, measurement.Id);
            Assert.True(measurement.IsComplete);
            Assert.Equal("Loaded", measurement.Title);
            context.ChangeTracker.VerifyNoOtherCalls();
        }

        private static ViewportMeasurementViewModel CreateCompletedMeasurement()
        {
            var measurement = new ViewportMeasurementViewModel(1, 2) { Title = "Length" };
            measurement.SetPixelSpacing(0.5, 0.5);
            measurement.Complete(3, 4);
            return measurement;
        }

        private sealed class TestContext
        {
            internal const string SeriesInstanceUid = "1.2.3.4";
            internal const string SopInstanceUid = "1.2.3.4.5";
            internal const string StudyInstanceUid = "1.2.3";

            internal TestContext()
            {
                ChangeTracker = new Mock<IImagingSessionChangeTracker>(MockBehavior.Strict);
                ChangeTracker.Setup(tracker => tracker.MarkChanged(It.IsAny<ImagingSessionChangeIdentity>(), It.IsAny<ImagingSessionChangeState>(), It.IsAny<ImagingMeasurementChangeData?>(), It.IsAny<ImagingMeasurementChangeData?>()));
                var imagingService = new Mock<IImagingService>();
                imagingService.SetupGet(service => service.ImagingSessionChangeTracker).Returns(ChangeTracker.Object);
                var core = new Mock<ICore>();
                core.SetupGet(service => service.ImagingService).Returns(imagingService.Object);
                Service = new ImagingMeasurementChangeService(core.Object);
            }

            internal Mock<IImagingSessionChangeTracker> ChangeTracker { get; }
            internal ImagingMeasurementChangeService Service { get; }
        }
    }
}