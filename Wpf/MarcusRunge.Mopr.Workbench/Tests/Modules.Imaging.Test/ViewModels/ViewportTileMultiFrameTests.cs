using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using Moq;
using System.Collections.ObjectModel;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class ViewportTileMultiFrameTests
    {
        [Fact]
        public void SetImagePositions_Should_Expose_All_Frames_Through_Existing_Slice_Api()
        {
            var tile = CreateTile();
            SeriesInfo series = new("Series", "US", "Multi-frame", string.Empty, 1, "Study");
            DicomImagePosition[] positions =
            [
                new("multi-frame.dcm", "1.2.3", 0, 3),
                new("multi-frame.dcm", "1.2.3", 1, 3),
                new("multi-frame.dcm", "1.2.3", 2, 3)
            ];

            tile.SetImagePositions(series, positions);
            tile.SetSlice(2);

            Assert.Equal(3, tile.SliceCount);
            Assert.Equal(2, tile.CurrentSlice);
            Assert.Equal(1, tile.CurrentImagePosition!.FrameIndex);
            Assert.Equal("multi-frame.dcm", tile.CurrentFilePath);
            Assert.Single(tile.SeriesFiles);
        }

        [Fact]
        public void MoveSlice_Should_Navigate_Between_Frames_Of_One_File()
        {
            var tile = CreateTile();
            SeriesInfo series = new("Series", "US", "Multi-frame", string.Empty, 1, "Study");
            tile.SetImagePositions(series,
            [
                new DicomImagePosition("multi-frame.dcm", "1.2.3", 0, 3),
                new DicomImagePosition("multi-frame.dcm", "1.2.3", 1, 3),
                new DicomImagePosition("multi-frame.dcm", "1.2.3", 2, 3)
            ]);

            tile.MoveSlice(1);
            tile.MoveSlice(1);
            tile.MoveSlice(1);

            Assert.Equal(3, tile.CurrentSlice);
            Assert.Equal(2, tile.CurrentImagePosition!.FrameIndex);
        }

        [Fact]
        public void CacheKey_Should_Distinguish_Frames_And_Ignore_Path_Casing()
        {
            var firstFrame = new DicomFrameCacheKey(@"C:\DICOM\INSTANCE.DCM", 0);
            var sameFrame = new DicomFrameCacheKey(@"c:\dicom\instance.dcm", 0);
            var secondFrame = new DicomFrameCacheKey(@"C:\DICOM\INSTANCE.DCM", 1);

            Assert.Equal(firstFrame, sameFrame);
            Assert.NotEqual(firstFrame, secondFrame);
        }

        private static ViewportTileViewModel CreateTile()
        {
            var store = new Mock<IImagingMeasurementSessionStore>();
            store.Setup(service => service.GetMeasurements(It.IsAny<ImagingMeasurementInstanceKey>())).Returns(() => new ObservableCollection<ViewportMeasurementViewModel>());
            var resolver = new Mock<IImagingMeasurementInstanceResolver>();
            resolver.Setup(service => service.Resolve(It.IsAny<SeriesInfo>(), It.IsAny<string>(), It.IsAny<int>())).Returns((SeriesInfo series, string filePath, int frameIndex) => new ImagingMeasurementInstanceKey(series.StudyId!, series.Id, filePath, frameIndex));
            return new ViewportTileViewModel("Viewport", "Viewport", store.Object, resolver.Object);
        }
    }
}
