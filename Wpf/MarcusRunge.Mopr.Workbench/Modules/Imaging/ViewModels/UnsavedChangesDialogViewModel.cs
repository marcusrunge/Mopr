using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using Prism.Commands;
using Prism.Dialogs;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    /// <summary>
    /// Presents the explicit save, discard and cancel choices for pending imaging changes.
    /// </summary>
    public sealed class UnsavedChangesDialogViewModel : IDialogAware
    {
        public const string DecisionParameterName = "Decision";

        private DelegateCommand? _cancelCommand, _discardCommand, _saveCommand;

        public DialogCloseListener RequestClose { get; }

        public DelegateCommand CancelCommand => _cancelCommand ??= new DelegateCommand(() => Close(UnsavedChangesDecision.Cancel, ButtonResult.Cancel));

        public DelegateCommand DiscardCommand => _discardCommand ??= new DelegateCommand(() => Close(UnsavedChangesDecision.Discard, ButtonResult.OK));

        public DelegateCommand SaveCommand => _saveCommand ??= new DelegateCommand(() => Close(UnsavedChangesDecision.Save, ButtonResult.OK));

        public string Title => Resources.UnsavedChangesDialog_Title;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        private void Close(UnsavedChangesDecision decision, ButtonResult buttonResult)
        {
            var parameters = new DialogParameters { { DecisionParameterName, decision } };
            RequestClose.Invoke(parameters, buttonResult);
        }
    }
}
