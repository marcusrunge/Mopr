using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Test
{
    public sealed class ImagingSessionServiceTests
    {
        [Fact]
        public void HasOpenStudy_Should_Follow_Current_Study()
        {
            var closedContext = new ImagingSessionServiceTestContext(hasOpenStudy: false);
            var openContext = new ImagingSessionServiceTestContext(hasOpenStudy: true);

            Assert.False(closedContext.Service.HasOpenStudy);
            Assert.True(openContext.Service.HasOpenStudy);
        }

        [Fact]
        public void HasUnsavedChanges_Should_Follow_Change_Tracker()
        {
            var cleanContext = new ImagingSessionServiceTestContext(hasUnsavedChanges: false);
            var dirtyContext = new ImagingSessionServiceTestContext(hasUnsavedChanges: true);

            Assert.False(cleanContext.Service.HasUnsavedChanges);
            Assert.True(dirtyContext.Service.HasUnsavedChanges);
        }

        [Fact]
        public void Clear_Should_Reject_Unsaved_Changes_Without_Modifying_Imaging_State()
        {
            var context = new ImagingSessionServiceTestContext(hasOpenStudy: true, hasUnsavedChanges: true);

            Assert.Throws<InvalidOperationException>(() => context.Service.Clear());

            context.StudyService.Verify(value => value.Clear(), Times.Never);
            context.SelectionService.Verify(value => value.SelectStudy(It.IsAny<StudyInfo?>()), Times.Never);
            context.SelectionService.Verify(value => value.SelectSeries(It.IsAny<SeriesInfo?>()), Times.Never);
            context.ViewportService.Verify(value => value.Reset(), Times.Never);
            context.WindowLevelService.Verify(value => value.ResetWindowLevelToDefault(), Times.Never);
            context.ToolService.Verify(value => value.ClearActiveTool(), Times.Never);
            context.ChangeTracker.Verify(value => value.Clear(), Times.Never);
        }

        [Fact]
        public void Clear_Should_Reset_All_Study_Bound_And_Transient_State()
        {
            var context = new ImagingSessionServiceTestContext(hasOpenStudy: true, hasUnsavedChanges: false);
            var eventCount = 0;
            context.Service.StateChanged += (_, _) => eventCount++;

            context.Service.Clear();

            context.StudyService.Verify(value => value.Clear(), Times.Once);
            context.SelectionService.Verify(value => value.ClearSelection(), Times.Once);
            context.ViewportService.Verify(value => value.Reset(), Times.Once);
            context.WindowLevelService.Verify(value => value.ResetWindowLevelToDefault(), Times.Once);
            context.ToolService.Verify(value => value.ClearActiveTool(), Times.Once);
            context.ChangeTracker.Verify(value => value.Clear(), Times.Once);
            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void Clear_Should_Be_Idempotent_When_No_Study_Is_Open()
        {
            var context = new ImagingSessionServiceTestContext(hasOpenStudy: false, hasUnsavedChanges: false);

            context.Service.Clear();
            context.Service.Clear();

            context.StudyService.Verify(value => value.Clear(), Times.Exactly(2));
            context.ViewportService.Verify(value => value.Reset(), Times.Exactly(2));
        }

        [Fact]
        public void StudyLoaded_Should_Raise_StateChanged()
        {
            var context = new ImagingSessionServiceTestContext();
            var eventCount = 0;
            context.Service.StateChanged += (_, _) => eventCount++;
            var study = new StudyInfo("study", "Study", "Description");

            context.StudyService.Raise(value => value.StudyLoaded += null, new ImagingStudyLoadedEventArgs(study, Array.Empty<SeriesInfo>(), null));

            Assert.Equal(1, eventCount);
        }

        [Fact]
        public void UnsavedChangesChanged_Should_Raise_StateChanged()
        {
            var context = new ImagingSessionServiceTestContext();
            var eventCount = 0;
            context.Service.StateChanged += (_, _) => eventCount++;

            context.ChangeTracker.Raise(value => value.UnsavedChangesChanged += null, EventArgs.Empty);

            Assert.Equal(1, eventCount);
        }
    }
}