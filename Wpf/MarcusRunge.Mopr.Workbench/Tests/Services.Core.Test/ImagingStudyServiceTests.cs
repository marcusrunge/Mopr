using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Core.Implementations.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Test
{
    public sealed class ImagingStudyServiceTests
    {
        [Fact]
        public async Task LoadStudyFromFilesAsync_Should_Load_Validated_Series_And_Raise_Event()
        {
            var context = CreateContext();
            string[] files = ["2.dcm", "1.dcm"];
            SetupMetadata(context.MetadataService, files[0], "study", "series", 2);
            SetupMetadata(context.MetadataService, files[1], "study", "series", 1);
            ImagingStudyLoadedEventArgs? eventArgs = null;
            context.Service.StudyLoaded += (_, value) => eventArgs = value;

            await context.Service.LoadStudyFromFilesAsync("study", "series", files, cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotNull(eventArgs);
            Assert.NotNull(context.Service.CurrentStudy);
            Assert.Single(context.Service.CurrentSeries);
            Assert.Equal(files, context.Service.GetFilesForSeries(context.Service.CurrentSeries[0].Id));
            Assert.Equal(2, context.Service.GetDicomMetadataForSeries(context.Service.CurrentSeries[0].Id).Count);
        }

        [Theory]
        [InlineData("", "series")]
        [InlineData("study", "")]
        public async Task LoadStudyFromFilesAsync_Should_Reject_Incomplete_Identity(string studyInstanceUid, string seriesInstanceUid)
        {
            var context = CreateContext();

            await Assert.ThrowsAsync<ArgumentException>(() => context.Service.LoadStudyFromFilesAsync(studyInstanceUid, seriesInstanceUid, ["1.dcm"], cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task LoadStudyFromFilesAsync_Should_Reject_Empty_File_List()
        {
            var context = CreateContext();

            await Assert.ThrowsAsync<ArgumentException>(() => context.Service.LoadStudyFromFilesAsync("study", "series", Array.Empty<string>(), cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task LoadStudyFromFilesAsync_Should_Reject_Non_Dicom_File()
        {
            var context = CreateContext();
            context.MetadataService.Setup(value => value.IsDicomFile("invalid.txt")).Returns(false);

            await Assert.ThrowsAsync<InvalidDataException>(() => context.Service.LoadStudyFromFilesAsync("study", "series", ["invalid.txt"], cancellationToken: TestContext.Current.CancellationToken));
        }

        [Theory]
        [InlineData("other-study", "series")]
        [InlineData("study", "other-series")]
        public async Task LoadStudyFromFilesAsync_Should_Reject_Mismatched_Dicom_Identity(string actualStudyInstanceUid, string actualSeriesInstanceUid)
        {
            var context = CreateContext();
            SetupMetadata(context.MetadataService, "1.dcm", actualStudyInstanceUid, actualSeriesInstanceUid, 1);

            await Assert.ThrowsAsync<InvalidDataException>(() => context.Service.LoadStudyFromFilesAsync("study", "series", ["1.dcm"], cancellationToken: TestContext.Current.CancellationToken));
            Assert.Null(context.Service.CurrentStudy);
            Assert.Empty(context.Service.CurrentSeries);
        }

        [Fact]
        public async Task LoadStudyFromFilesAsync_Should_Keep_Current_Study_When_Validation_Fails()
        {
            var context = CreateContext();
            SetupMetadata(context.MetadataService, "valid.dcm", "study", "series", 1);
            await context.Service.LoadStudyFromFilesAsync("study", "series", ["valid.dcm"], cancellationToken: TestContext.Current.CancellationToken);
            var currentStudy = context.Service.CurrentStudy;
            context.MetadataService.Setup(value => value.IsDicomFile("invalid.dcm")).Returns(false);

            await Assert.ThrowsAsync<InvalidDataException>(() => context.Service.LoadStudyFromFilesAsync("other-study", "other-series", ["invalid.dcm"], cancellationToken: TestContext.Current.CancellationToken));

            Assert.Same(currentStudy, context.Service.CurrentStudy);
            Assert.Single(context.Service.CurrentSeries);
        }

        [Fact]
        public async Task LoadStudyFromFilesAsync_Should_Propagate_Cancellation()
        {
            var context = CreateContext();
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Service.LoadStudyFromFilesAsync("study", "series", ["1.dcm"], cancellationToken: cancellation.Token));
        }

        private static TestContextData CreateContext()
        {
            var metadataService = new Mock<IDicomMetadataService>(MockBehavior.Strict);
            var dicom = new Mock<IDicom>(MockBehavior.Strict);
            var coreBase = new Mock<ICoreBase>(MockBehavior.Strict);
            var imagingServiceBase = new Mock<IImagingServiceBase>(MockBehavior.Strict);
            dicom.SetupGet(value => value.MetadataService).Returns(metadataService.Object);
            coreBase.SetupGet(value => value.Dicom).Returns(dicom.Object);
            imagingServiceBase.SetupGet(value => value.CoreBase).Returns(coreBase.Object);
            IImagingStudyService service = ImagingStudyService.Create(imagingServiceBase.Object, CreationLifetime.Scoped) ?? throw new InvalidOperationException("The imaging study service could not be created.");
            return new TestContextData(service, metadataService);
        }

        private static void SetupMetadata(Mock<IDicomMetadataService> metadataService, string filePath, string studyInstanceUid, string seriesInstanceUid, int instanceNumber)
        {
            metadataService.Setup(value => value.IsDicomFile(filePath)).Returns(true);
            metadataService.Setup(value => value.ReadMetadataAsync(filePath, It.IsAny<CancellationToken>())).ReturnsAsync(new DicomFileMetadata(filePath, studyInstanceUid, seriesInstanceUid, $"sop-{instanceNumber}", "CT", "Study", "Series", instanceNumber, 512, 512, patientName: "Patient", seriesNumber: 2));
        }

        private sealed record TestContextData(IImagingStudyService Service, Mock<IDicomMetadataService> MetadataService);
    }
}