using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.Services
{
    public sealed class ImagingMeasurementSessionStoreTests
    {
        [Fact]
        public void GetMeasurements_Should_Return_Same_Collection_For_Same_Instance()
        {
            var context = new TestContext();

            var first = context.Store.GetMeasurements(TestContext.InstanceKey);
            var second = context.Store.GetMeasurements(TestContext.InstanceKey);

            Assert.Same(first, second);
        }

        [Fact]
        public void Create_Should_Add_Measurement_And_Register_Created_Change()
        {
            var context = new TestContext();
            var measurement = CreateMeasurement();

            context.Store.Create(TestContext.InstanceKey, measurement);

            Assert.Same(measurement, Assert.Single(context.Store.GetMeasurements(TestContext.InstanceKey)));
            context.ChangeService.Verify(service => service.MarkCreated(measurement, TestContext.InstanceKey.StudyInstanceUid, TestContext.InstanceKey.SeriesInstanceUid, TestContext.InstanceKey.SopInstanceUid), Times.Once);
        }

        [Fact]
        public void Load_Should_Not_Register_Change()
        {
            var context = new TestContext();
            var measurement = CreateMeasurement();

            context.Store.Load(TestContext.InstanceKey, measurement);

            Assert.Single(context.Store.GetMeasurements(TestContext.InstanceKey));
            context.ChangeService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Title_Change_Should_Register_Modified_Change()
        {
            var context = new TestContext();
            var measurement = CreateMeasurement();
            context.Store.Load(TestContext.InstanceKey, measurement);

            measurement.Title = "Updated";

            context.ChangeService.Verify(service => service.MarkModified(measurement, It.Is<ViewportMeasurementViewModel>(original => original.Id == measurement.Id && original.Title == "Original"), TestContext.InstanceKey.StudyInstanceUid, TestContext.InstanceKey.SeriesInstanceUid, TestContext.InstanceKey.SopInstanceUid), Times.Once);
        }

        [Fact]
        public void Delete_Should_Remove_Measurement_And_Register_Deleted_Change()
        {
            var context = new TestContext();
            var measurement = CreateMeasurement();
            context.Store.Load(TestContext.InstanceKey, measurement);

            bool removed = context.Store.Delete(TestContext.InstanceKey, measurement);

            Assert.True(removed);
            Assert.Empty(context.Store.GetMeasurements(TestContext.InstanceKey));
            context.ChangeService.Verify(service => service.MarkDeleted(measurement, TestContext.InstanceKey.StudyInstanceUid, TestContext.InstanceKey.SeriesInstanceUid, TestContext.InstanceKey.SopInstanceUid), Times.Once);
        }

        [Fact]
        public void Clear_Should_Unsubscribe_Loaded_Measurements()
        {
            var context = new TestContext();
            var measurement = CreateMeasurement();
            context.Store.Load(TestContext.InstanceKey, measurement);

            context.Store.Clear();
            measurement.Title = "Updated";

            context.ChangeService.VerifyNoOtherCalls();
            Assert.Empty(context.Store.GetMeasurements(TestContext.InstanceKey));
        }

        private static ViewportMeasurementViewModel CreateMeasurement()
        {
            var measurement = new ViewportMeasurementViewModel(1, 2) { Title = "Original" };
            measurement.Complete(3, 4);
            return measurement;
        }

        private sealed class TestContext
        {
            internal static readonly ImagingMeasurementInstanceKey InstanceKey = new("1.2.3", "1.2.3.4", "1.2.3.4.5");

            internal TestContext()
            {
                ChangeService = new Mock<IImagingMeasurementChangeService>(MockBehavior.Strict);
                ChangeService.Setup(service => service.MarkCreated(It.IsAny<ViewportMeasurementViewModel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
                ChangeService.Setup(service => service.MarkModified(It.IsAny<ViewportMeasurementViewModel>(), It.IsAny<ViewportMeasurementViewModel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
                ChangeService.Setup(service => service.MarkDeleted(It.IsAny<ViewportMeasurementViewModel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
                Store = new ImagingMeasurementSessionStore(ChangeService.Object);
            }

            internal Mock<IImagingMeasurementChangeService> ChangeService { get; }
            internal ImagingMeasurementSessionStore Store { get; }
        }
    }
}