using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.Services
{
    public sealed class ViewportSliceSynchronizationCompatibilityServiceTests
    {
        [Fact]
        public void AreCompatible_Should_Accept_Identical_Ordered_Dicom_Identity()
        {
            var service = CreateService(CreateMetadata("source", "1", "2"), CreateMetadata("target", "1", "2"));

            Assert.True(service.AreCompatible(CreateContext("viewport-1", "source", "source-1.dcm", "source-2.dcm"), CreateContext("viewport-2", "target", "target-1.dcm", "target-2.dcm")));
        }

        [Fact]
        public void AreCompatible_Should_Reject_Reordered_Sop_Sequence()
        {
            var service = CreateService(CreateMetadata("source", "1", "2"), CreateMetadata("target", "2", "1"));

            Assert.False(service.AreCompatible(CreateContext("viewport-1", "source", "source-1.dcm", "source-2.dcm"), CreateContext("viewport-2", "target", "target-1.dcm", "target-2.dcm")));
        }

        [Theory]
        [InlineData(null, "series", "1")]
        [InlineData("study", null, "1")]
        [InlineData("study", "series", null)]
        public void AreCompatible_Should_Reject_Missing_Dicom_Identity(string? studyUid, string? seriesUid, string? sopUid)
        {
            var source = new[] { Metadata("source-1.dcm", studyUid, seriesUid, sopUid) };
            var service = CreateService(source, CreateMetadata("target", "1"));

            Assert.False(service.AreCompatible(CreateContext("viewport-1", "source", "source-1.dcm"), CreateContext("viewport-2", "target", "target-1.dcm")));
        }

        [Fact]
        public void AreCompatible_Should_Reject_Different_Series()
        {
            var source = new[] { Metadata("source-1.dcm", "study", "series-a", "1") };
            var target = new[] { Metadata("target-1.dcm", "study", "series-b", "1") };
            var service = CreateService(source, target);

            Assert.False(service.AreCompatible(CreateContext("viewport-1", "source", "source-1.dcm"), CreateContext("viewport-2", "target", "target-1.dcm")));
        }

        [Fact]
        public void AreCompatible_Should_Reject_Different_Viewport_Counts()
        {
            var service = CreateService(CreateMetadata("source", "1", "2"), CreateMetadata("target", "1"));

            Assert.False(service.AreCompatible(CreateContext("viewport-1", "source", "source-1.dcm", "source-2.dcm"), CreateContext("viewport-2", "target", "target-1.dcm")));
        }

        [Fact]
        public void AreCompatible_Should_Reject_Metadata_Not_Covering_Ordered_File_List()
        {
            var service = CreateService(CreateMetadata("source", "1", "2"), CreateMetadata("target", "1", "2"));

            Assert.False(service.AreCompatible(CreateContext("viewport-1", "source", "source-1.dcm", "source-unknown.dcm"), CreateContext("viewport-2", "target", "target-1.dcm", "target-2.dcm")));
        }

        private static ViewportSliceSynchronizationContext CreateContext(string viewportId, string seriesId, params string[] files) => new(viewportId, seriesId, files, 1, files.Length);
        private static DicomFileMetadata[] CreateMetadata(string prefix, params string[] sopUids) => [.. sopUids.Select((sopUid, index) => Metadata($"{prefix}-{index + 1}.dcm", "study", "series", sopUid))];
        private static DicomFileMetadata Metadata(string path, string? studyUid, string? seriesUid, string? sopUid) => new(path, studyUid, seriesUid, sopUid, "CT", null, null, null, 512, 512);

        private static ViewportSliceSynchronizationCompatibilityService CreateService(IReadOnlyList<DicomFileMetadata> source, IReadOnlyList<DicomFileMetadata> target)
        {
            var studyService = new Mock<IImagingStudyService>();
            var imagingService = new Mock<IImagingService>();
            var core = new Mock<ICore>();
            studyService.Setup(service => service.GetDicomMetadataForSeries("source")).Returns(source);
            studyService.Setup(service => service.GetDicomMetadataForSeries("target")).Returns(target);
            imagingService.SetupGet(service => service.ImagingStudyService).Returns(studyService.Object);
            core.SetupGet(service => service.ImagingService).Returns(imagingService.Object);
            return new ViewportSliceSynchronizationCompatibilityService(core.Object);
        }
    }
}

