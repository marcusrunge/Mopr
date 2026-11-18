using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Imaging;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class ImagingCommandBarSaveTests
    {
        [Fact]
        public void SaveCommand_Should_Require_Open_Study_And_Unsaved_Changes()
        {
            var withoutStudy = new ImagingCommandBarViewModelTestContext(hasOpenStudy: false, hasUnsavedChanges: true);
            var withoutChanges = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: false);
            var ready = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);

            Assert.False(withoutStudy.ViewModel.SaveCommand.CanExecute());
            Assert.False(withoutChanges.ViewModel.SaveCommand.CanExecute());
            Assert.True(ready.ViewModel.SaveCommand.CanExecute());
        }

        [Fact]
        public async Task SaveCommand_Should_Save_And_Show_Completed_Status()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);

            context.ImagingSessionService
                .SetupSequence(service => service.HasUnsavedChanges)
                .Returns(true)
                .Returns(false);

            context.ImagingSessionApplicationService
                .Setup(service => service.SaveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ImagingSessionSaveResult(1, 1));

            context.ViewModel.SaveCommand.Execute();

            await WaitUntilAsync(
                () => !context.ViewModel.IsBusy && context.ViewModel.StatusText != Resources.CommandBar_Status,
                Xunit.TestContext.Current.CancellationToken);

            Assert.Equal(Resources.CommandBar_SaveCompleted, context.ViewModel.StatusText);

            context.ImagingSessionApplicationService.Verify(
                service => service.SaveAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SaveCommand_Should_Report_Error()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);
            var exception = new InvalidOperationException("Save failed.");

            context.ImagingSessionApplicationService
                .Setup(service => service.SaveAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            context.Application
                .Setup(service => service.ReportException(exception));

            context.ViewModel.SaveCommand.Execute();

            await WaitUntilAsync(
                () => !context.ViewModel.IsBusy && context.ViewModel.StatusText != Resources.CommandBar_Status,
                Xunit.TestContext.Current.CancellationToken);

            Assert.Equal(Resources.CommandBar_SaveFailed, context.ViewModel.StatusText);

            context.Application.Verify(
                service => service.ReportException(exception),
                Times.Once);
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