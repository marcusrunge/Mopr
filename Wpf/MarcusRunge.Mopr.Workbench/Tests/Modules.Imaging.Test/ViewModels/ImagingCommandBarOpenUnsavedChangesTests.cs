using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies;
using Moq;
using System.Reflection;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class ImagingCommandBarOpenUnsavedChangesTests
    {
        private const string SelectedSeriesRequestsParameterName = "SelectedSeriesRequests";
        private const string UnsavedChangesDialogName = "UnsavedChangesDialogView";

        [Fact]
        public async Task OpenImportedSeriesAsync_Should_Keep_Current_Study_When_Unsaved_Changes_Are_Not_Discarded()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);
            ImportedSeriesLoadRequest[] requests = [new(17, "series-1")];
            ImportedStudyLoadResult loadResult = CreateSuccessfulLoadResult();

            context.ImportedSeriesLoader.Setup(value => value.ResolveAsync(It.Is<IReadOnlyList<ImportedSeriesLoadRequest>>(items => items.SequenceEqual(requests)), It.IsAny<CancellationToken>())).ReturnsAsync(loadResult);

            ConfigureUnsavedChangesDialog(context, ButtonResult.Cancel);

            var parameters = new DialogParameters
            {
                { SelectedSeriesRequestsParameterName, requests }
            };

            await InvokeOpenImportedSeriesAsync(context.ViewModel, ImagingCommandBarViewModelTestContext.CreateDialogResult(parameters).Object);

            context.ImportedSeriesLoader.Verify(value => value.ResolveAsync(It.Is<IReadOnlyList<ImportedSeriesLoadRequest>>(items => items.SequenceEqual(requests)), It.IsAny<CancellationToken>()), Times.Once);

            VerifyUnsavedChangesDialogWasShown(context);
            context.ImagingSessionService.Verify(value => value.DiscardChangesAndClear(), Times.Never);
            context.ImagingSessionService.Verify(value => value.Clear(), Times.Never);

            context.ImagingStudyService.Verify(value => value.LoadStudyFromSeriesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ImagingSeriesLoadRequest>>(), It.IsAny<IProgress<ImagingStudyLoadProgress>?>(), It.IsAny<CancellationToken>()), Times.Never);

            Assert.False(context.ViewModel.IsBusy);
        }

        [Fact]
        public async Task OpenImportedSeriesAsync_Should_Keep_Current_Study_When_Unsaved_Changes_Dialog_Is_Closed()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);
            ImportedSeriesLoadRequest[] requests = [new(17, "series-1")];

            context.ImportedSeriesLoader.Setup(value => value.ResolveAsync(It.IsAny<IReadOnlyList<ImportedSeriesLoadRequest>>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreateSuccessfulLoadResult());

            ConfigureUnsavedChangesDialog(context, ButtonResult.None);

            var parameters = new DialogParameters
            {
                { SelectedSeriesRequestsParameterName, requests }
            };

            await InvokeOpenImportedSeriesAsync(context.ViewModel, ImagingCommandBarViewModelTestContext.CreateDialogResult(parameters).Object);

            VerifyUnsavedChangesDialogWasShown(context);
            context.ImagingSessionService.Verify(value => value.DiscardChangesAndClear(), Times.Never);

            context.ImagingStudyService.Verify(value => value.LoadStudyFromSeriesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ImagingSeriesLoadRequest>>(), It.IsAny<IProgress<ImagingStudyLoadProgress>?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task OpenImportedSeriesAsync_Should_Discard_Changes_After_Resolution_And_Load_New_Study_When_Confirmed()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);
            ImportedSeriesLoadRequest[] requests = [new(17, "series-1")];
            ImportedStudyLoadResult loadResult = CreateSuccessfulLoadResult();
            var invocationOrder = new List<string>();

            context.ImportedSeriesLoader.Setup(value => value.ResolveAsync(It.IsAny<IReadOnlyList<ImportedSeriesLoadRequest>>(), It.IsAny<CancellationToken>())).Callback(() => invocationOrder.Add("Resolve")).ReturnsAsync(loadResult);

            context.ImagingSessionService.Setup(value => value.DiscardChangesAndClear()).Callback(() => invocationOrder.Add("Discard"));

            context.ImagingStudyService.Setup(value => value.LoadStudyFromSeriesAsync("study", It.Is<IReadOnlyList<ImagingSeriesLoadRequest>>(items => items.Count == 1 && items[0].SeriesInstanceUid == "series-1"), It.IsAny<IProgress<ImagingStudyLoadProgress>?>(), It.IsAny<CancellationToken>())).Callback(() => invocationOrder.Add("Load")).Returns(Task.CompletedTask);

            ConfigureUnsavedChangesDialog(context, ButtonResult.OK);

            var parameters = new DialogParameters
            {
                { SelectedSeriesRequestsParameterName, requests }
            };

            await InvokeOpenImportedSeriesAsync(context.ViewModel, ImagingCommandBarViewModelTestContext.CreateDialogResult(parameters).Object);

            VerifyUnsavedChangesDialogWasShown(context);
            context.ImagingSessionService.Verify(value => value.Clear(), Times.Never);
            context.ImagingSessionService.Verify(value => value.DiscardChangesAndClear(), Times.Once);

            context.ImagingStudyService.Verify(value => value.LoadStudyFromSeriesAsync("study", It.Is<IReadOnlyList<ImagingSeriesLoadRequest>>(items => items.Count == 1 && items[0].SeriesInstanceUid == "series-1"), It.IsAny<IProgress<ImagingStudyLoadProgress>?>(), It.IsAny<CancellationToken>()), Times.Once);

            Assert.Equal(["Resolve", "Discard", "Load"], invocationOrder);
            Assert.False(context.ViewModel.IsBusy);
        }

        [Fact]
        public async Task OpenImportedSeriesAsync_Should_Not_Request_Discard_When_Resolution_Fails()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);
            ImportedSeriesLoadRequest[] requests = [new(17, "series-1")];

            context.ImportedSeriesLoader.Setup(value => value.ResolveAsync(It.IsAny<IReadOnlyList<ImportedSeriesLoadRequest>>(), It.IsAny<CancellationToken>())).ReturnsAsync(ImportedStudyLoadResult.Failed("Technical test failure."));

            var parameters = new DialogParameters
            {
                { SelectedSeriesRequestsParameterName, requests }
            };

            await InvokeOpenImportedSeriesAsync(context.ViewModel, ImagingCommandBarViewModelTestContext.CreateDialogResult(parameters).Object);

            VerifyUnsavedChangesDialogWasNotShown(context);
            context.ImagingSessionService.Verify(value => value.DiscardChangesAndClear(), Times.Never);

            context.ImagingStudyService.Verify(value => value.LoadStudyFromSeriesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ImagingSeriesLoadRequest>>(), It.IsAny<IProgress<ImagingStudyLoadProgress>?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task OpenImportedSeriesAsync_Should_Load_Clean_Session_Without_Unsaved_Changes_Dialog()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: false);
            ImportedSeriesLoadRequest[] requests = [new(17, "series-1")];

            context.ImportedSeriesLoader.Setup(value => value.ResolveAsync(It.IsAny<IReadOnlyList<ImportedSeriesLoadRequest>>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreateSuccessfulLoadResult());

            context.ImagingStudyService.Setup(value => value.LoadStudyFromSeriesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<ImagingSeriesLoadRequest>>(), It.IsAny<IProgress<ImagingStudyLoadProgress>?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var parameters = new DialogParameters
            {
                { SelectedSeriesRequestsParameterName, requests }
            };

            await InvokeOpenImportedSeriesAsync(context.ViewModel, ImagingCommandBarViewModelTestContext.CreateDialogResult(parameters).Object);

            VerifyUnsavedChangesDialogWasNotShown(context);
            context.ImagingSessionService.Verify(value => value.DiscardChangesAndClear(), Times.Never);

            context.ImagingStudyService.Verify(value => value.LoadStudyFromSeriesAsync("study", It.IsAny<IReadOnlyList<ImagingSeriesLoadRequest>>(), It.IsAny<IProgress<ImagingStudyLoadProgress>?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private static ImportedStudyLoadResult CreateSuccessfulLoadResult()
        {
            ImportedSeriesLoadResult series = ImportedSeriesLoadResult.Succeeded(17, "series-1", "study", ["series-1-1.dcm"]);

            return ImportedStudyLoadResult.Succeeded("study", [series]);
        }

        private static void ConfigureUnsavedChangesDialog(ImagingCommandBarViewModelTestContext context, ButtonResult buttonResult) => context.DialogService.Setup(service => service.ShowDialog(UnsavedChangesDialogName, It.IsAny<IDialogParameters>(), It.IsAny<DialogCallback>())).Callback<string, IDialogParameters, DialogCallback>((_, _, callback) =>
        {
            Mock<IDialogResult> result = ImagingCommandBarViewModelTestContext.CreateDialogResult(new DialogParameters(), buttonResult);

            callback.Invoke(result.Object).GetAwaiter().GetResult();
        });

        private static void VerifyUnsavedChangesDialogWasShown(ImagingCommandBarViewModelTestContext context) => context.DialogService.Verify(service => service.ShowDialog(UnsavedChangesDialogName, It.IsAny<IDialogParameters>(), It.IsAny<DialogCallback>()), Times.Once);

        private static void VerifyUnsavedChangesDialogWasNotShown(ImagingCommandBarViewModelTestContext context) => context.DialogService.Verify(service => service.ShowDialog(UnsavedChangesDialogName, It.IsAny<IDialogParameters>(), It.IsAny<DialogCallback>()), Times.Never);

        private static Task InvokeOpenImportedSeriesAsync(ImagingCommandBarViewModel viewModel, IDialogResult dialogResult)
        {
            MethodInfo method = typeof(ImagingCommandBarViewModel).GetMethod("OpenImportedSeriesAsync", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingMethodException(typeof(ImagingCommandBarViewModel).FullName, "OpenImportedSeriesAsync");

            return method.Invoke(viewModel, [dialogResult]) as Task ?? throw new InvalidOperationException("OpenImportedSeriesAsync did not return a Task.");
        }
    }
}