using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using Prism.Commands;
using Prism.Dialogs;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    /// <summary>
    /// Requests an explicit decision before unsaved imaging changes are discarded.
    /// </summary>
    public sealed class UnsavedChangesDialogViewModel : ViewModelBase, IDialogAware
    {
        public UnsavedChangesDialogViewModel()
        {
            CancelCommand = new DelegateCommand(Cancel);
            DiscardCommand = new DelegateCommand(Discard);
        }

        public DelegateCommand CancelCommand { get; }
        public DelegateCommand DiscardCommand { get; }
        public DialogCloseListener RequestClose { get; }
        public static string Title => Properties.Resources.UnsavedChangesDialog_Title;
        public bool CanCloseDialog() => true;
        public void OnDialogClosed() { }
        public void OnDialogOpened(IDialogParameters parameters) { }
        private void Cancel() => RequestClose.Invoke(ButtonResult.Cancel);
        private void Discard() => RequestClose.Invoke(ButtonResult.OK);
    }
}