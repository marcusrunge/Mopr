using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Services;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.Services
{
    public sealed class ViewportSliceSynchronizationCompatibilityServiceTests
    {
        private readonly ViewportSliceSynchronizationCompatibilityService _service = new();

        [Fact]
        public void AreCompatible_Should_Accept_Identical_Ordered_Single_Frame_Identity()
        {
            Assert.True(_service.AreCompatible(CreateContext("viewport-1", Position("source-1.dcm", "1", 0), Position("source-2.dcm", "2", 0)), CreateContext("viewport-2", Position("target-1.dcm", "1", 0), Position("target-2.dcm", "2", 0))));
        }

        [Fact]
        public void AreCompatible_Should_Accept_Identical_Ordered_Multi_Frame_Identity()
        {
            Assert.True(_service.AreCompatible(CreateContext("viewport-1", Position("source.dcm", "1", 0, 3), Position("source.dcm", "1", 1, 3), Position("source.dcm", "1", 2, 3)), CreateContext("viewport-2", Position("target.dcm", "1", 0, 3), Position("target.dcm", "1", 1, 3), Position("target.dcm", "1", 2, 3))));
        }

        [Fact]
        public void AreCompatible_Should_Reject_Reordered_Sop_Sequence()
        {
            Assert.False(_service.AreCompatible(CreateContext("viewport-1", Position("source-1.dcm", "1", 0), Position("source-2.dcm", "2", 0)), CreateContext("viewport-2", Position("target-1.dcm", "2", 0), Position("target-2.dcm", "1", 0))));
        }

        [Fact]
        public void AreCompatible_Should_Reject_Reordered_Frame_Sequence_Within_One_Sop_Instance()
        {
            Assert.False(_service.AreCompatible(CreateContext("viewport-1", Position("source.dcm", "1", 0, 2), Position("source.dcm", "1", 1, 2)), CreateContext("viewport-2", Position("target.dcm", "1", 1, 2), Position("target.dcm", "1", 0, 2))));
        }

        [Fact]
        public void AreCompatible_Should_Reject_Same_Sop_Sequence_With_Different_Frame_Index()
        {
            Assert.False(_service.AreCompatible(CreateContext("viewport-1", Position("source.dcm", "1", 0, 2), Position("source.dcm", "1", 1, 2)), CreateContext("viewport-2", Position("target.dcm", "1", 0, 2), Position("target.dcm", "1", 0, 2))));
        }

        [Fact]
        public void AreCompatible_Should_Reject_Different_Frame_Counts()
        {
            Assert.False(_service.AreCompatible(CreateContext("viewport-1", Position("source.dcm", "1", 0, 2), Position("source.dcm", "1", 1, 2)), CreateContext("viewport-2", Position("target.dcm", "1", 0, 1))));
        }

        [Fact]
        public void AreCompatible_Should_Reject_Same_Viewport()
        {
            var context = CreateContext("viewport-1", Position("source.dcm", "1", 0));
            Assert.False(_service.AreCompatible(context, context));
        }

        [Fact]
        public void AreCompatible_Should_Reject_Missing_Sop_Instance_Uid() => Assert.False(_service.AreCompatible(CreateContext("viewport-1", Position("source.dcm", null, 0)), CreateContext("viewport-2", Position("target.dcm", "1", 0))));

        private static ViewportSliceSynchronizationContext CreateContext(string viewportId, params DicomImagePosition[] positions) => new(viewportId, "series", positions, 1, positions.Length);
        private static DicomImagePosition Position(string path, string? sopInstanceUid, int frameIndex, int frameCount = 1) => new(path, sopInstanceUid, frameIndex, frameCount);
    }
}
