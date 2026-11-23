using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Imaging;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class ImagingCommandBarSaveGuardTests
    {
        private const string UnsavedChangesDialogName = "UnsavedChangesDialogView";

        [Fact]
        public async Task CloseStudyCommand_Should_Save_And_Close_When_No_Changes_Remain()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);
            context.ImagingSessionService.SetupSequence(service => service.HasUnsavedChanges).Returns(true).Returns(false);
            context.ImagingSessionApplicationService.Setup(service => service.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new ImagingSessionSaveResult(1, 1));
            ConfigureDecision(context, UnsavedChangesDecision.Save);

            context.ViewModel.CloseStudyCommand.Execute();
            await WaitUntilAsync(() => context.ViewModel.StatusText == Resources.CommandBar_StudyClosed, Xunit.TestContext.Current.CancellationToken);

            context.ImagingSessionApplicationService.Verify(service => service.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
            context.ImagingSessionService.Verify(service => service.Clear(), Times.Once);
            context.ImagingSessionService.Verify(service => service.DiscardChangesAndClear(), Times.Never);
        }

        [Fact]
        public async Task CloseStudyCommand_Should_Remain_Open_When_Newer_Changes_Exist_After_Save()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);
            context.ImagingSessionService.SetupGet(service => service.HasUnsavedChanges).Returns(true);
            context.ImagingSessionApplicationService.Setup(service => service.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new ImagingSessionSaveResult(1, 0));
            ConfigureDecision(context, UnsavedChangesDecision.Save);

            context.ViewModel.CloseStudyCommand.Execute();
            await WaitUntilAsync(() => context.ViewModel.StatusText == Resources.CommandBar_SaveCompletedWithRemainingChanges, Xunit.TestContext.Current.CancellationToken);

            context.ImagingSessionService.Verify(service => service.Clear(), Times.Never);
            context.ImagingSessionService.Verify(service => service.DiscardChangesAndClear(), Times.Never);
        }

        [Fact]
        public async Task CloseStudyCommand_Should_Remain_Open_When_Save_Fails()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);
            var exception = new InvalidOperationException("Save failed.");
            context.ImagingSessionApplicationService.Setup(service => service.SaveAsync(It.IsAny<CancellationToken>())).ThrowsAsync(exception);
            context.Application.Setup(service => service.ReportException(exception));
            ConfigureDecision(context, UnsavedChangesDecision.Save);

            context.ViewModel.CloseStudyCommand.Execute();
            await WaitUntilAsync(() => context.ViewModel.StatusText == Resources.CommandBar_SaveFailed, Xunit.TestContext.Current.CancellationToken);

            context.Application.Verify(service => service.ReportException(exception), Times.Once);
            context.ImagingSessionService.Verify(service => service.Clear(), Times.Never);
            context.ImagingSessionService.Verify(service => service.DiscardChangesAndClear(), Times.Never);
        }

        [Fact]
        public async Task CloseStudyCommand_Should_Remain_Open_When_Save_Is_Canceled()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);
            context.ImagingSessionApplicationService.Setup(service => service.SaveAsync(It.IsAny<CancellationToken>())).Returns<CancellationToken>(token => Task.FromCanceled<ImagingSessionSaveResult>(new CancellationToken(canceled: true)));
            ConfigureDecision(context, UnsavedChangesDecision.Save);

            context.ViewModel.CloseStudyCommand.Execute();
            await WaitUntilAsync(() => context.ViewModel.StatusText == Resources.CommandBar_SaveCanceled || context.ViewModel.StatusText == Resources.CommandBar_SaveFailed, Xunit.TestContext.Current.CancellationToken);

            context.ImagingSessionService.Verify(service => service.Clear(), Times.Never);
            context.ImagingSessionService.Verify(service => service.DiscardChangesAndClear(), Times.Never);
        }

        private static void ConfigureDecision(ImagingCommandBarViewModelTestContext context, UnsavedChangesDecision decision)
        {
            context.DialogService.Setup(service => service.ShowDialog(UnsavedChangesDialogName, It.IsAny<IDialogParameters>(), It.IsAny<DialogCallback>())).Callback<string, IDialogParameters, DialogCallback>((_, _, callback) =>
            {
                var parameters = new DialogParameters { { UnsavedChangesDialogViewModel.DecisionParameterName, decision } };
                Mock<IDialogResult> result = ImagingCommandBarViewModelTestContext.CreateDialogResult(parameters, decision == UnsavedChangesDecision.Cancel ? ButtonResult.Cancel : ButtonResult.OK);
                callback.Invoke(result.Object).GetAwaiter().GetResult();
            });
        }

        private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
        {
            while (!condition())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(10, cancellationToken);
            }
        }
    }
}