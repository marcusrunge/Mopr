using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Geometry;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Imaging;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.Services
{
    public sealed class ImagingMeasurementHydrationServiceTests
    {
        [Fact]
        public async Task HydrateAsync_Should_Load_Supported_Measurement_Without_Creating_Change()
        {
            var context = new TestContext();

            await context.Service.HydrateAsync(TestContext.StudyInstanceUid, TestContext.CurrentCancellationToken);

            context.SessionStore.Verify(store => store.Clear(), Times.Once);
            context.SessionStore.Verify(store => store.Load(It.Is<ImagingMeasurementInstanceKey>(key => key.SopInstanceUid == TestContext.SopInstanceUid), context.ViewModel), Times.Once);
            context.ChangeService.Verify(service => service.CreateLoadedMeasurement(context.PersistedMeasurement.EntityId, 1, 2, 3, 4, null, null, "Length", "Loaded", context.PersistedMeasurement.CreatedAtUtc, context.PersistedMeasurement.ModifiedAtUtc), Times.Once);
        }

        [Fact]
        public async Task HydrateAsync_Should_Not_Clear_Current_State_When_Payload_Is_Unsupported()
        {
            var context = new TestContext(MeasurementType.Angle, new AngleMeasurementData());

            await Assert.ThrowsAsync<NotSupportedException>(() => context.Service.HydrateAsync(TestContext.StudyInstanceUid, TestContext.CurrentCancellationToken));

            context.SessionStore.Verify(store => store.Clear(), Times.Never);
            context.SessionStore.Verify(store => store.Load(It.IsAny<ImagingMeasurementInstanceKey>(), It.IsAny<ViewportMeasurementViewModel>()), Times.Never);
        }

        [Fact]
        public async Task HydrateAsync_Should_Propagate_Cancellation_Without_Clearing_Current_State()
        {
            var context = new TestContext();
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Service.HydrateAsync(TestContext.StudyInstanceUid, cancellationTokenSource.Token));

            context.SessionStore.Verify(store => store.Clear(), Times.Never);
        }

        private sealed class TestContext
        {
            internal const string SeriesInstanceUid = "1.2.3.4";
            internal const string SopInstanceUid = "1.2.3.4.5";
            internal const string StudyInstanceUid = "1.2.3";
            internal static CancellationToken CurrentCancellationToken => Xunit.TestContext.Current.CancellationToken;

            internal TestContext(MeasurementType measurementType = MeasurementType.Length, MeasurementData? data = null)
            {
                data ??= new LengthMeasurementData { StartPoint = new Point2D { X = 1, Y = 2 }, EndPoint = new Point2D { X = 3, Y = 4 } };
                PersistedMeasurement = new LoadedImagingMeasurement(Guid.NewGuid(), StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, measurementType, data, "Length", "Loaded", 1, DateTime.UtcNow.AddMinutes(-5), 1, DateTime.UtcNow);
                var applicationService = new Mock<IImagingSessionApplicationService>();
                applicationService.Setup(service => service.LoadMeasurementsAsync(StudyInstanceUid, It.IsAny<CancellationToken>())).ReturnsAsync([PersistedMeasurement]);
                var application = new Mock<IApplication>();
                application.SetupGet(service => service.ImagingSession).Returns(applicationService.Object);
                ViewModel = new ViewportMeasurementViewModel(1, 2);
                ViewModel.Complete(3, 4);
                ChangeService = new Mock<IImagingMeasurementChangeService>(MockBehavior.Strict);
                ChangeService.Setup(service => service.CreateLoadedMeasurement(It.IsAny<Guid>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>())).Returns(ViewModel);
                SessionStore = new Mock<IImagingMeasurementSessionStore>(MockBehavior.Strict);
                SessionStore.Setup(store => store.Clear());
                SessionStore.Setup(store => store.Load(It.IsAny<ImagingMeasurementInstanceKey>(), It.IsAny<ViewportMeasurementViewModel>()));
                Service = new ImagingMeasurementHydrationService(application.Object, ChangeService.Object, SessionStore.Object);
            }

            internal Mock<IImagingMeasurementChangeService> ChangeService { get; }
            internal LoadedImagingMeasurement PersistedMeasurement { get; }
            internal Mock<IImagingMeasurementSessionStore> SessionStore { get; }
            internal ImagingMeasurementHydrationService Service { get; }
            internal ViewportMeasurementViewModel ViewModel { get; }
        }
    }
}