using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using Moq;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class ImagingCommandBarCloseStudyTests
    {
        private const string UnsavedChangesDialogName = "UnsavedChangesDialogView";

        [Fact]
        public void CloseStudyCommand_Should_Be_Disabled_When_No_Study_Is_Open()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: false);

            Assert.False(context.ViewModel.CloseStudyCommand.CanExecute());

            context.ViewModel.CloseStudyCommand.Execute();

            VerifyDialogWasNotShown(context);
            context.ImagingSessionService.Verify(service => service.Clear(), Times.Never);
            context.ImagingSessionService.Verify(service => service.DiscardChangesAndClear(), Times.Never);
        }

        [Fact]
        public void CloseStudyCommand_Should_Clear_Open_Clean_Study_Without_Dialog()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: false);

            Assert.True(context.ViewModel.CloseStudyCommand.CanExecute());

            context.ViewModel.CloseStudyCommand.Execute();

            VerifyDialogWasNotShown(context);
            context.ImagingSessionService.Verify(service => service.Clear(), Times.Once);
            context.ImagingSessionService.Verify(service => service.DiscardChangesAndClear(), Times.Never);

            Assert.Equal(Resources.CommandBar_StudyClosed, context.ViewModel.StatusText);
        }

        [Fact]
        public void CloseStudyCommand_Should_Show_Unsaved_Changes_Dialog_For_Dirty_Study()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);

            ConfigureDialogResult(context, ButtonResult.Cancel);

            context.ViewModel.CloseStudyCommand.Execute();

            VerifyUnsavedChangesDialogWasShown(context);
        }

        [Fact]
        public void CloseStudyCommand_Should_Keep_Dirty_Study_Open_When_User_Cancels()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);
            string originalStatusText = context.ViewModel.StatusText;

            ConfigureDialogResult(context, ButtonResult.Cancel);

            context.ViewModel.CloseStudyCommand.Execute();

            VerifyUnsavedChangesDialogWasShown(context);
            context.ImagingSessionService.Verify(service => service.Clear(), Times.Never);
            context.ImagingSessionService.Verify(service => service.DiscardChangesAndClear(), Times.Never);

            Assert.Equal(originalStatusText, context.ViewModel.StatusText);
        }

        [Fact]
        public void CloseStudyCommand_Should_Keep_Dirty_Study_Open_When_Dialog_Is_Closed()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);
            string originalStatusText = context.ViewModel.StatusText;

            ConfigureDialogResult(context, ButtonResult.None);

            context.ViewModel.CloseStudyCommand.Execute();

            VerifyUnsavedChangesDialogWasShown(context);
            context.ImagingSessionService.Verify(service => service.Clear(), Times.Never);
            context.ImagingSessionService.Verify(service => service.DiscardChangesAndClear(), Times.Never);

            Assert.Equal(originalStatusText, context.ViewModel.StatusText);
        }

        [Fact]
        public void CloseStudyCommand_Should_Discard_Changes_And_Clear_When_User_Confirms()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);

            ConfigureDialogResult(context, ButtonResult.OK);

            context.ViewModel.CloseStudyCommand.Execute();

            VerifyUnsavedChangesDialogWasShown(context);
            context.ImagingSessionService.Verify(service => service.Clear(), Times.Never);
            context.ImagingSessionService.Verify(service => service.DiscardChangesAndClear(), Times.Once);

            Assert.Equal(Resources.CommandBar_StudyClosed, context.ViewModel.StatusText);
        }

        [Fact]
        public void CloseStudyCommand_Should_Be_Disabled_While_Command_Bar_Is_Busy()
        {
            var context = new ImagingCommandBarViewModelTestContext(hasOpenStudy: true, hasUnsavedChanges: true);

            SetIsBusy(context, true);

            Assert.False(context.ViewModel.CloseStudyCommand.CanExecute());

            context.ViewModel.CloseStudyCommand.Execute();

            VerifyDialogWasNotShown(context);
            context.ImagingSessionService.Verify(service => service.Clear(), Times.Never);
            context.ImagingSessionService.Verify(service => service.DiscardChangesAndClear(), Times.Never);
        }

        private static void ConfigureDialogResult(ImagingCommandBarViewModelTestContext context, ButtonResult buttonResult) => context.DialogService
                .Setup(service => service.ShowDialog(UnsavedChangesDialogName, It.IsAny<IDialogParameters>(), It.IsAny<DialogCallback>())).Callback<string, IDialogParameters, DialogCallback>((_, _, callback) =>
                {
                    Mock<IDialogResult> result = ImagingCommandBarViewModelTestContext.CreateDialogResult(new DialogParameters(), buttonResult);

                    callback.Invoke(result.Object).GetAwaiter().GetResult();
                });

        private static void VerifyUnsavedChangesDialogWasShown(ImagingCommandBarViewModelTestContext context) => context.DialogService.Verify(service => service.ShowDialog(
                                                                                                                              UnsavedChangesDialogName,
                                                                                                                              It.IsAny<IDialogParameters>(),
                                                                                                                              It.IsAny<DialogCallback>()), Times.Once);

        private static void VerifyDialogWasNotShown(ImagingCommandBarViewModelTestContext context) => context.DialogService.Verify(service => service.ShowDialog(
                                                                                                                   It.IsAny<string>(),
                                                                                                                   It.IsAny<IDialogParameters>(),
                                                                                                                   It.IsAny<DialogCallback>()), Times.Never);

        private static void SetIsBusy(ImagingCommandBarViewModelTestContext context, bool value)
        {
            var property = typeof(Modules.Imaging.ViewModels.ImagingCommandBarViewModel).GetProperty(nameof(Modules.Imaging.ViewModels.ImagingCommandBarViewModel.IsBusy));

            Assert.NotNull(property);

            property.SetValue(context.ViewModel, value);
        }
    }
}