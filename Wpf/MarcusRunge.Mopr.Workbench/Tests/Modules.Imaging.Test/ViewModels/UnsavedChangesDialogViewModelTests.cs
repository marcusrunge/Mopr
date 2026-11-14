using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class UnsavedChangesDialogViewModelTests
    {
        [Fact]
        public void Title_Should_Use_Localized_Resource() => Assert.Equal(Resources.UnsavedChangesDialog_Title, UnsavedChangesDialogViewModel.Title);

        [Fact]
        public void CanCloseDialog_Should_Return_True()
        {
            var viewModel = new UnsavedChangesDialogViewModel();

            Assert.True(viewModel.CanCloseDialog());
        }

        [Fact]
        public void Commands_Should_Be_Executable()
        {
            var viewModel = new UnsavedChangesDialogViewModel();

            Assert.True(viewModel.CancelCommand.CanExecute());
            Assert.True(viewModel.DiscardCommand.CanExecute());
        }

        [Fact]
        public void OnDialogOpened_Should_Preserve_State()
        {
            var viewModel = new UnsavedChangesDialogViewModel();

            viewModel.OnDialogOpened(new DialogParameters());

            Assert.True(viewModel.CanCloseDialog());
            Assert.True(viewModel.CancelCommand.CanExecute());
            Assert.True(viewModel.DiscardCommand.CanExecute());
            Assert.Equal(Resources.UnsavedChangesDialog_Title, UnsavedChangesDialogViewModel.Title);
        }

        [Fact]
        public void OnDialogClosed_Should_Preserve_State()
        {
            var viewModel = new UnsavedChangesDialogViewModel();

            viewModel.OnDialogClosed();

            Assert.True(viewModel.CanCloseDialog());
            Assert.True(viewModel.CancelCommand.CanExecute());
            Assert.True(viewModel.DiscardCommand.CanExecute());
        }
    }
}