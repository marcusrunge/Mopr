using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class SeriesPanelViewModelTests
    {
        [Fact]
        public void SearchText_Should_Filter_By_Name_Description_Modality_Number_And_Id()
        {
            var context = new TestContext();
            SeriesInfo[] series = [CreateSeries("series-1", "Axial 1 mm", "Lung kernel", "CT", 1), CreateSeries("series-2", "Coronal", "Soft tissue", "MR", 2), CreateSeries("series-3", "Scout", "Localizer", "DX", 3)];
            context.RaiseStudyLoaded(series);

            context.ViewModel.SearchText = "lung CT 1";

            SeriesInfo result = Assert.Single(context.ViewModel.Series);
            Assert.Equal("series-1", result.Id);
            Assert.Same(result, context.ViewModel.SelectedSeries);
        }

        [Fact]
        public void SearchText_Should_Leave_Visible_List_Empty_When_No_Series_Matches()
        {
            var context = new TestContext();
            context.RaiseStudyLoaded([CreateSeries("series-1", "Axial", "Lung", "CT", 1)]);

            context.ViewModel.SearchText = "NO-MATCH-4711";

            Assert.Empty(context.ViewModel.Series);
            Assert.Null(context.ViewModel.SelectedSeries);
            context.SelectionService.Verify(value => value.SelectSeries(null), Times.Once);
        }

        [Fact]
        public void Clearing_SearchText_Should_Restore_All_Series()
        {
            var context = new TestContext();
            SeriesInfo[] series = [CreateSeries("series-1", "Axial", "Lung", "CT", 1), CreateSeries("series-2", "Coronal", "Soft tissue", "MR", 2)];
            context.RaiseStudyLoaded(series);
            context.ViewModel.SearchText = "Axial";

            context.ViewModel.SearchText = string.Empty;

            Assert.Equal(2, context.ViewModel.Series.Count);
        }

        [Fact]
        public void SearchText_Should_Preserve_Selected_Series_When_It_Remains_Visible()
        {
            var context = new TestContext();
            SeriesInfo[] series = [CreateSeries("series-1", "Axial lung", "Lung", "CT", 1), CreateSeries("series-2", "Axial bone", "Bone", "CT", 2)];
            context.RaiseStudyLoaded(series);
            context.ViewModel.SelectedSeries = series[1];

            context.ViewModel.SearchText = "Axial";

            Assert.Same(series[1], context.ViewModel.SelectedSeries);
        }

        [Fact]
        public void StudyLoaded_Should_Apply_Existing_Filter_To_New_Study()
        {
            var context = new TestContext();
            context.ViewModel.SearchText = "MR";

            context.RaiseStudyLoaded([CreateSeries("series-1", "Axial", "Lung", "CT", 1), CreateSeries("series-2", "Coronal", "Soft tissue", "MR", 2)]);

            SeriesInfo result = Assert.Single(context.ViewModel.Series);
            Assert.Equal("series-2", result.Id);
        }

        private static SeriesInfo CreateSeries(string id, string name, string description, string modality, int seriesNumber) => new(id, modality, name, description, 10, "study", seriesNumber);

        private sealed class TestContext
        {
            public TestContext()
            {
                Core.SetupGet(value => value.ImagingService).Returns(ImagingService.Object);
                ImagingService.SetupGet(value => value.ImagingSelectionService).Returns(SelectionService.Object);
                ImagingService.SetupGet(value => value.ImagingStudyService).Returns(StudyService.Object);
                StudyService.SetupGet(value => value.CurrentStudy).Returns((StudyInfo?)null);
                StudyService.SetupGet(value => value.CurrentSeries).Returns(Array.Empty<SeriesInfo>());
                ViewModel = new SeriesPanelViewModel(Core.Object);
                SelectionService.Invocations.Clear();
            }

            public Mock<ICore> Core { get; } = new();
            public Mock<IImagingService> ImagingService { get; } = new();
            public Mock<IImagingSelectionService> SelectionService { get; } = new();
            public Mock<IImagingStudyService> StudyService { get; } = new();
            public SeriesPanelViewModel ViewModel { get; }

            public void RaiseStudyLoaded(IReadOnlyList<SeriesInfo> series)
            {
                var study = new StudyInfo("study", "Study", "Description");
                StudyService.Raise(value => value.StudyLoaded += null, new ImagingStudyLoadedEventArgs(study, series, null));
            }
        }
    }
}