using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.Services
{
    public sealed class ImagingMeasurementInstanceResolverTests
    {
        [Fact]
        public void Resolve_Should_Return_Stable_Dicom_Identity_For_Displayed_File()
        {
            var context = new TestContext();

            ImagingMeasurementInstanceKey result = context.Resolver.Resolve(context.Series, TestContext.FilePath);

            Assert.Equal(TestContext.StudyInstanceUid, result.StudyInstanceUid);
            Assert.Equal(TestContext.SeriesInstanceUid, result.SeriesInstanceUid);
            Assert.Equal(TestContext.SopInstanceUid, result.SopInstanceUid);
        }

        [Fact]
        public void Resolve_Should_Reject_File_Without_Registered_Metadata()
        {
            var context = new TestContext();

            Assert.Throws<InvalidOperationException>(() => context.Resolver.Resolve(context.Series, @"C:\missing.dcm"));
        }

        [Fact]
        public void Resolve_Should_Reject_Mismatched_Series()
        {
            var context = new TestContext();
            var wrongSeries = new SeriesInfo("9.9.9", "CT", "Wrong", string.Empty, 1, TestContext.StudyInstanceUid);

            Assert.Throws<InvalidOperationException>(() => context.Resolver.Resolve(wrongSeries, TestContext.FilePath));
        }

        private sealed class TestContext
        {
            internal const string FilePath = @"C:\study\instance.dcm";
            internal const string SeriesInstanceUid = "1.2.3.4";
            internal const string SopInstanceUid = "1.2.3.4.5";
            internal const string StudyInstanceUid = "1.2.3";

            internal TestContext()
            {
                Series = new SeriesInfo(SeriesInstanceUid, "CT", "Series", string.Empty, 1, StudyInstanceUid);
                var metadata = new DicomFileMetadata(FilePath, StudyInstanceUid, SeriesInstanceUid, SopInstanceUid, "CT", null, null, 1, 512, 512);
                var studyService = new Mock<IImagingStudyService>();
                studyService.Setup(service => service.GetDicomMetadataForSeries(It.IsAny<string>())).Returns([metadata]);
                var imagingService = new Mock<IImagingService>();
                imagingService.SetupGet(service => service.ImagingStudyService).Returns(studyService.Object);
                var core = new Mock<ICore>();
                core.SetupGet(service => service.ImagingService).Returns(imagingService.Object);
                Resolver = new ImagingMeasurementInstanceResolver(core.Object);
            }

            internal ImagingMeasurementInstanceResolver Resolver { get; }
            internal SeriesInfo Series { get; }
        }
    }
}
