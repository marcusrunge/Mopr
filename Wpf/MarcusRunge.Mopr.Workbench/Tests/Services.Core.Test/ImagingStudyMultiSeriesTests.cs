using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Core.Implementations.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Test
{
    public sealed class ImagingStudyMultiSeriesTests
    {
        [Fact]
        public async Task LoadStudyFromSeriesAsync_Should_Load_All_Selected_Series_Atomically()
        {
            var context = CreateContext();
            SetupMetadata(context.MetadataService, "series-1-2.dcm", "study", "series-1", 2, 1);
            SetupMetadata(context.MetadataService, "series-1-1.dcm", "study", "series-1", 1, 1);
            SetupMetadata(context.MetadataService, "series-2-1.dcm", "study", "series-2", 1, 2);
            ImagingSeriesLoadRequest[] requests = [new("series-1", ["series-1-2.dcm", "series-1-1.dcm"]), new("series-2", ["series-2-1.dcm"])];

            await context.Service.LoadStudyFromSeriesAsync("study", requests, cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotNull(context.Service.CurrentStudy);
            Assert.Equal(2, context.Service.CurrentSeries.Count);
            Assert.Equal(2, context.Service.GetFilesForSeries(context.Service.CurrentSeries[0].Id).Count);
            Assert.Single(context.Service.GetFilesForSeries(context.Service.CurrentSeries[1].Id));
        }

        [Fact]
        public async Task LoadStudyFromSeriesAsync_Should_Reject_Empty_Selection()
        {
            var context = CreateContext();

            await Assert.ThrowsAsync<ArgumentException>(() => context.Service.LoadStudyFromSeriesAsync("study", [], cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task LoadStudyFromSeriesAsync_Should_Reject_Duplicate_Series_Uid()
        {
            var context = CreateContext();
            SetupMetadata(context.MetadataService, "1.dcm", "study", "series", 1, 1);
            SetupMetadata(context.MetadataService, "2.dcm", "study", "series", 2, 1);
            ImagingSeriesLoadRequest[] requests = [new("series", ["1.dcm"]), new("series", ["2.dcm"])];

            await Assert.ThrowsAsync<InvalidDataException>(() => context.Service.LoadStudyFromSeriesAsync("study", requests, cancellationToken: TestContext.Current.CancellationToken));
            Assert.Null(context.Service.CurrentStudy);
            Assert.Empty(context.Service.CurrentSeries);
        }

        [Fact]
        public async Task LoadStudyFromSeriesAsync_Should_Reject_Series_Without_Files()
        {
            var context = CreateContext();

            await Assert.ThrowsAsync<ArgumentException>(() => context.Service.LoadStudyFromSeriesAsync("study", [new ImagingSeriesLoadRequest("series", [])], cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task LoadStudyFromSeriesAsync_Should_Keep_Previous_Study_When_Any_Selected_Series_Fails()
        {
            var context = CreateContext();
            SetupMetadata(context.MetadataService, "existing.dcm", "existing-study", "existing-series", 1, 1);
            await context.Service.LoadStudyFromSeriesAsync("existing-study", [new ImagingSeriesLoadRequest("existing-series", ["existing.dcm"])], cancellationToken: TestContext.Current.CancellationToken);
            var existingStudy = context.Service.CurrentStudy;
            string existingSeriesId = Assert.Single(context.Service.CurrentSeries).Id;
            SetupMetadata(context.MetadataService, "valid.dcm", "new-study", "new-series-1", 1, 1);
            context.MetadataService.Setup(value => value.IsDicomFile("invalid.dcm")).Returns(false);
            ImagingSeriesLoadRequest[] requests = [new("new-series-1", ["valid.dcm"]), new("new-series-2", ["invalid.dcm"])];

            await Assert.ThrowsAsync<InvalidDataException>(() => context.Service.LoadStudyFromSeriesAsync("new-study", requests, cancellationToken: TestContext.Current.CancellationToken));

            Assert.Same(existingStudy, context.Service.CurrentStudy);
            Assert.Equal(existingSeriesId, Assert.Single(context.Service.CurrentSeries).Id);
            Assert.Equal(["existing.dcm"], context.Service.GetFilesForSeries(existingSeriesId));
        }

        [Fact]
        public async Task LoadStudyFromSeriesAsync_Should_Propagate_Cancellation()
        {
            var context = CreateContext();
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Service.LoadStudyFromSeriesAsync("study", [new ImagingSeriesLoadRequest("series", ["1.dcm"])], cancellationToken: cancellation.Token));
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

        private static void SetupMetadata(Mock<IDicomMetadataService> metadataService, string filePath, string studyInstanceUid, string seriesInstanceUid, int instanceNumber, int seriesNumber)
        {
            metadataService.Setup(value => value.IsDicomFile(filePath)).Returns(true);
            metadataService.Setup(value => value.ReadMetadataAsync(filePath, It.IsAny<CancellationToken>())).ReturnsAsync(new DicomFileMetadata(filePath, studyInstanceUid, seriesInstanceUid, $"sop-{seriesInstanceUid}-{instanceNumber}", "CT", "Study", $"Series {seriesNumber}", instanceNumber, 512, 512, patientName: "Patient", seriesNumber: seriesNumber));
        }

        private sealed record TestContextData(IImagingStudyService Service, Mock<IDicomMetadataService> MetadataService);
    }
}