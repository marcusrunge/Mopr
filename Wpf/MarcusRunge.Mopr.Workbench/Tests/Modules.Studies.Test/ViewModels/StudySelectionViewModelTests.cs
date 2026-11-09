using MarcusRunge.Mopr.Workbench.Modules.Studies.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;
using Moq;

namespace MarcusRunge.Mopr.Workbench.Modules.Studies.Test.ViewModels
{
    public sealed class StudySelectionViewModelTests
    {
        [Fact]
        public async Task LoadAsync_Should_Load_Studies_And_Select_First_Study()
        {
            var context = new StudySelectionViewModelTestContext();
            StudyCatalogItem[] studies = [StudySelectionViewModelTestContext.CreateStudy(1), StudySelectionViewModelTestContext.CreateStudy(2)];
            context.StudyCatalogService.Setup(value => value.GetStudiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(studies);

            await StudySelectionViewModelTestContext.InvokeLoadAsync(context.ViewModel);

            Assert.Equal(2, context.ViewModel.Studies.Count);
            Assert.Same(context.ViewModel.Studies[0], context.ViewModel.SelectedStudy);
            Assert.True(context.ViewModel.HasStudies);
            Assert.False(context.ViewModel.ShowEmptyState);
            Assert.False(context.ViewModel.HasLoadError);
            Assert.True(context.ViewModel.ConfirmCommand.CanExecute());
        }

        [Fact]
        public async Task LoadAsync_Should_Show_Empty_State_For_Empty_Catalog()
        {
            var context = new StudySelectionViewModelTestContext();
            context.StudyCatalogService.Setup(value => value.GetStudiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<StudyCatalogItem>());

            await StudySelectionViewModelTestContext.InvokeLoadAsync(context.ViewModel);

            Assert.Empty(context.ViewModel.Studies);
            Assert.Null(context.ViewModel.SelectedStudy);
            Assert.False(context.ViewModel.HasStudies);
            Assert.True(context.ViewModel.ShowEmptyState);
            Assert.False(context.ViewModel.HasLoadError);
            Assert.False(context.ViewModel.ConfirmCommand.CanExecute());
        }

        [Fact]
        public async Task ExecuteLoad_Should_Show_User_Friendly_Error_Without_Technical_Details()
        {
            var context = new StudySelectionViewModelTestContext();
            const string technicalMessage = "SQL connection failed for server SECRET-SQL.";
            context.StudyCatalogService.Setup(value => value.GetStudiesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException(technicalMessage));

            StudySelectionViewModelTestContext.InvokeExecuteLoad(context.ViewModel);
            await StudySelectionViewModelTestContext.WaitUntilAsync(() => context.ViewModel.HasLoadError && !context.ViewModel.IsLoading, TestContext.Current.CancellationToken);

            Assert.True(context.ViewModel.HasLoadError);
            Assert.False(context.ViewModel.ShowEmptyState);
            Assert.NotEmpty(context.ViewModel.ErrorMessage);
            Assert.DoesNotContain(technicalMessage, context.ViewModel.ErrorMessage, StringComparison.Ordinal);
            Assert.True(context.ViewModel.RetryCommand.CanExecute());
            Assert.False(context.ViewModel.ConfirmCommand.CanExecute());
        }

        [Fact]
        public async Task RetryCommand_Should_Clear_Error_And_Load_Studies()
        {
            var context = new StudySelectionViewModelTestContext();
            StudyCatalogItem[] studies = [StudySelectionViewModelTestContext.CreateStudy()];
            var calls = 0;
            context.StudyCatalogService.Setup(value => value.GetStudiesAsync(It.IsAny<CancellationToken>())).Returns(() =>
            {
                calls++;
                return calls == 1 ? Task.FromException<IReadOnlyList<StudyCatalogItem>>(new IOException("Temporary database failure.")) : Task.FromResult<IReadOnlyList<StudyCatalogItem>>(studies);
            });
            StudySelectionViewModelTestContext.InvokeExecuteLoad(context.ViewModel);
            await StudySelectionViewModelTestContext.WaitUntilAsync(() => context.ViewModel.HasLoadError && !context.ViewModel.IsLoading, TestContext.Current.CancellationToken);

            Assert.True(context.ViewModel.RetryCommand.CanExecute());
            context.ViewModel.RetryCommand.Execute();
            await StudySelectionViewModelTestContext.WaitUntilAsync(() => context.ViewModel.HasStudies && !context.ViewModel.IsLoading, TestContext.Current.CancellationToken);

            Assert.Equal(2, calls);
            Assert.False(context.ViewModel.HasLoadError);
            Assert.Empty(context.ViewModel.ErrorMessage);
            Assert.False(context.ViewModel.RetryCommand.CanExecute());
            Assert.Single(context.ViewModel.Studies);
            Assert.True(context.ViewModel.ConfirmCommand.CanExecute());
        }

        [Fact]
        public async Task Destroy_Should_Cancel_Load_Without_Error_State()
        {
            var context = new StudySelectionViewModelTestContext();
            var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            context.StudyCatalogService.Setup(value => value.GetStudiesAsync(It.IsAny<CancellationToken>())).Returns(async (CancellationToken cancellationToken) =>
            {
                operationStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return Array.Empty<StudyCatalogItem>();
            });
            StudySelectionViewModelTestContext.InvokeExecuteLoad(context.ViewModel);
            await operationStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            context.ViewModel.Destroy();
            await StudySelectionViewModelTestContext.WaitUntilAsync(() => !context.ViewModel.IsLoading, TestContext.Current.CancellationToken);

            Assert.False(context.ViewModel.HasLoadError);
            Assert.Empty(context.ViewModel.ErrorMessage);
        }

        [Fact]
        public void StudyItemViewModel_Should_Select_All_Available_Series_By_Default()
        {
            SeriesCatalogItem[] series = [StudySelectionViewModelTestContext.CreateSeries(11), StudySelectionViewModelTestContext.CreateSeries(12), StudySelectionViewModelTestContext.CreateSeries(13, isAvailable: false)];
            var viewModel = new StudyItemViewModel(StudySelectionViewModelTestContext.CreateStudy(series: series));

            Assert.Equal(2, viewModel.AvailableSeriesCount);
            Assert.Equal(2, viewModel.SelectedSeriesCount);
            Assert.True(viewModel.Series[0].IsSelected);
            Assert.True(viewModel.Series[1].IsSelected);
            Assert.False(viewModel.Series[2].IsSelected);
            Assert.True(viewModel.HasSelectedSeries);
        }

        [Fact]
        public void StudyItemViewModel_Should_Clear_And_Restore_Available_Selection()
        {
            SeriesCatalogItem[] series = [StudySelectionViewModelTestContext.CreateSeries(11), StudySelectionViewModelTestContext.CreateSeries(12), StudySelectionViewModelTestContext.CreateSeries(13, isAvailable: false)];
            var viewModel = new StudyItemViewModel(StudySelectionViewModelTestContext.CreateStudy(series: series));

            viewModel.ClearSelection();

            Assert.Equal(0, viewModel.SelectedSeriesCount);
            Assert.False(viewModel.HasSelectedSeries);

            viewModel.SelectAllAvailable();

            Assert.Equal(2, viewModel.SelectedSeriesCount);
            Assert.True(viewModel.HasSelectedSeries);
            Assert.False(viewModel.Series[2].IsSelected);
        }
    }
}