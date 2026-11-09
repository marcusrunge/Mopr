using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;
using Moq;
using Prism.Dialogs;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class ImagingCommandBarViewModelTests
    {
        private const string SelectedSeriesRequestsParameterName = "SelectedSeriesRequests";

        [Fact]
        public async Task OpenImportedSeriesAsync_Should_Load_All_Selected_Series()
        {
            var context = new ImagingCommandBarViewModelTestContext();
            ImportedSeriesLoadRequest[] requests = [new(17, "series-1"), new(18, "series-2")];
            ImportedSeriesLoadResult firstSeries = ImportedSeriesLoadResult.Succeeded(17, "series-1", "study", ["series-1-1.dcm"]);
            ImportedSeriesLoadResult secondSeries = ImportedSeriesLoadResult.Succeeded(18, "series-2", "study", ["series-2-1.dcm"]);
            ImportedStudyLoadResult loadResult = ImportedStudyLoadResult.Succeeded("study", [firstSeries, secondSeries]);
            context.ImportedSeriesLoader.Setup(value => value.ResolveAsync(It.Is<IReadOnlyList<ImportedSeriesLoadRequest>>(items => items.SequenceEqual(requests)), It.IsAny<CancellationToken>())).ReturnsAsync(loadResult);
            context.ImagingStudyService.Setup(value => value.LoadStudyFromSeriesAsync("study", It.Is<IReadOnlyList<ImagingSeriesLoadRequest>>(items => items.Count == 2 && items[0].SeriesInstanceUid == "series-1" && items[1].SeriesInstanceUid == "series-2"), It.IsAny<IProgress<ImagingStudyLoadProgress>?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var parameters = new DialogParameters { { SelectedSeriesRequestsParameterName, requests } };

            await InvokeOpenImportedSeriesAsync(context.ViewModel, ImagingCommandBarViewModelTestContext.CreateDialogResult(parameters).Object);

            context.ImportedSeriesLoader.Verify(value => value.ResolveAsync(It.Is<IReadOnlyList<ImportedSeriesLoadRequest>>(items => items.SequenceEqual(requests)), It.IsAny<CancellationToken>()), Times.Once);
            context.ImagingStudyService.Verify(value => value.LoadStudyFromSeriesAsync("study", It.Is<IReadOnlyList<ImagingSeriesLoadRequest>>(items => items.Count == 2), It.IsAny<IProgress<ImagingStudyLoadProgress>?>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.False(context.ViewModel.IsBusy);
            Assert.False(context.ViewModel.CancelOpenCommand.CanExecute());
        }

        [Fact]
        public async Task OpenImportedSeriesAsync_Should_Not_Invoke_Loader_For_Empty_Selection()
        {
            var context = new ImagingCommandBarViewModelTestContext();
            var parameters = new DialogParameters { { SelectedSeriesRequestsParameterName, Array.Empty<ImportedSeriesLoadRequest>() } };

            await InvokeOpenImportedSeriesAsync(context.ViewModel, ImagingCommandBarViewModelTestContext.CreateDialogResult(parameters).Object);

            context.ImportedSeriesLoader.Verify(value => value.ResolveAsync(It.IsAny<IReadOnlyList<ImportedSeriesLoadRequest>>(), It.IsAny<CancellationToken>()), Times.Never);
            context.ImagingStudyService.Verify(value => value.LoadStudyFromSeriesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ImagingSeriesLoadRequest>>(), It.IsAny<IProgress<ImagingStudyLoadProgress>?>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.False(context.ViewModel.IsBusy);
        }

        [Fact]
        public async Task OpenImportedSeriesAsync_Should_Not_Invoke_Imaging_When_Loader_Fails()
        {
            var context = new ImagingCommandBarViewModelTestContext();
            ImportedSeriesLoadRequest[] requests = [new(17, "series-1")];
            context.ImportedSeriesLoader.Setup(value => value.ResolveAsync(It.IsAny<IReadOnlyList<ImportedSeriesLoadRequest>>(), It.IsAny<CancellationToken>())).ReturnsAsync(ImportedStudyLoadResult.Failed("Technical test failure."));
            var parameters = new DialogParameters { { SelectedSeriesRequestsParameterName, requests } };

            await InvokeOpenImportedSeriesAsync(context.ViewModel, ImagingCommandBarViewModelTestContext.CreateDialogResult(parameters).Object);

            context.ImagingStudyService.Verify(value => value.LoadStudyFromSeriesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ImagingSeriesLoadRequest>>(), It.IsAny<IProgress<ImagingStudyLoadProgress>?>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.DoesNotContain("Technical test failure", context.ViewModel.StatusText, StringComparison.Ordinal);
            Assert.False(context.ViewModel.IsBusy);
        }

        [Fact]
        public async Task CancelOpenCommand_Should_Cancel_Blocking_ImportedSeries_Load()
        {
            var context = new ImagingCommandBarViewModelTestContext();
            ImportedSeriesLoadRequest[] requests = [new(17, "series-1")];
            var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationToken observedCancellationToken = default;
            context.ImportedSeriesLoader.Setup(value => value.ResolveAsync(It.IsAny<IReadOnlyList<ImportedSeriesLoadRequest>>(), It.IsAny<CancellationToken>())).Returns(async (IReadOnlyList<ImportedSeriesLoadRequest> _, CancellationToken cancellationToken) =>
            {
                observedCancellationToken = cancellationToken;
                operationStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return ImportedStudyLoadResult.Failed("The operation should have been cancelled.");
            });
            var parameters = new DialogParameters { { SelectedSeriesRequestsParameterName, requests } };
            Task openTask = InvokeOpenImportedSeriesAsync(context.ViewModel, ImagingCommandBarViewModelTestContext.CreateDialogResult(parameters).Object);
            await operationStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            Assert.True(context.ViewModel.IsBusy);
            Assert.True(context.ViewModel.CancelOpenCommand.CanExecute());
            context.ViewModel.CancelOpenCommand.Execute();
            await openTask.WaitAsync(TestContext.Current.CancellationToken);

            Assert.True(observedCancellationToken.IsCancellationRequested);
            Assert.False(context.ViewModel.IsBusy);
            Assert.False(context.ViewModel.CancelOpenCommand.CanExecute());
            context.ImagingStudyService.Verify(value => value.LoadStudyFromSeriesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ImagingSeriesLoadRequest>>(), It.IsAny<IProgress<ImagingStudyLoadProgress>?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task OpenImportedSeriesAsync_Should_Block_Parallel_Open_While_Busy()
        {
            var context = new ImagingCommandBarViewModelTestContext();
            ImportedSeriesLoadRequest[] requests = [new(17, "series-1")];
            var operationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var continueOperation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ImportedSeriesLoadResult series = ImportedSeriesLoadResult.Succeeded(17, "series-1", "study", ["1.dcm"]);
            context.ImportedSeriesLoader.Setup(value => value.ResolveAsync(It.IsAny<IReadOnlyList<ImportedSeriesLoadRequest>>(), It.IsAny<CancellationToken>())).Returns(async (IReadOnlyList<ImportedSeriesLoadRequest> _, CancellationToken cancellationToken) =>
            {
                operationStarted.TrySetResult();
                await continueOperation.Task.WaitAsync(cancellationToken);
                return ImportedStudyLoadResult.Succeeded("study", [series]);
            });
            context.ImagingStudyService.Setup(value => value.LoadStudyFromSeriesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ImagingSeriesLoadRequest>>(), It.IsAny<IProgress<ImagingStudyLoadProgress>?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var parameters = new DialogParameters { { SelectedSeriesRequestsParameterName, requests } };
            Mock<IDialogResult> dialogResult = ImagingCommandBarViewModelTestContext.CreateDialogResult(parameters);
            Task firstOpenTask = InvokeOpenImportedSeriesAsync(context.ViewModel, dialogResult.Object);
            await operationStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            await InvokeOpenImportedSeriesAsync(context.ViewModel, dialogResult.Object);
            continueOperation.TrySetResult();
            await firstOpenTask.WaitAsync(TestContext.Current.CancellationToken);

            context.ImportedSeriesLoader.Verify(value => value.ResolveAsync(It.IsAny<IReadOnlyList<ImportedSeriesLoadRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
            context.ImagingStudyService.Verify(value => value.LoadStudyFromSeriesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ImagingSeriesLoadRequest>>(), It.IsAny<IProgress<ImagingStudyLoadProgress>?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private static Task InvokeOpenImportedSeriesAsync(ImagingCommandBarViewModel viewModel, IDialogResult dialogResult)
        {
            MethodInfo method = typeof(ImagingCommandBarViewModel).GetMethod("OpenImportedSeriesAsync", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingMethodException(typeof(ImagingCommandBarViewModel).FullName, "OpenImportedSeriesAsync");
            return method.Invoke(viewModel, [dialogResult]) as Task ?? throw new InvalidOperationException("OpenImportedSeriesAsync did not return a Task.");
        }
    }
}