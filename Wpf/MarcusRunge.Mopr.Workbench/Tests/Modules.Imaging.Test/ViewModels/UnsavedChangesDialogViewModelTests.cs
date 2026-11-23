using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels;
using System.Reflection;
using Xunit;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Test.ViewModels
{
    public sealed class UnsavedChangesDialogViewModelTests
    {
        [Theory]
        [InlineData(UnsavedChangesDecision.Save, ButtonResult.OK)]
        [InlineData(UnsavedChangesDecision.Discard, ButtonResult.OK)]
        [InlineData(UnsavedChangesDecision.Cancel, ButtonResult.Cancel)]
        public void Decision_Command_Should_Close_With_Explicit_Decision(UnsavedChangesDecision expectedDecision, ButtonResult expectedResult)
        {
            var viewModel = new UnsavedChangesDialogViewModel();
            IDialogResult? actualResult = null;

            SetRequestCloseListener(viewModel, dialogResult => actualResult = dialogResult);

            ExecuteDecisionCommand(viewModel, expectedDecision);

            Assert.NotNull(actualResult);
            Assert.Equal(expectedResult, actualResult.Result);
            Assert.True(actualResult.Parameters.TryGetValue(UnsavedChangesDialogViewModel.DecisionParameterName, out UnsavedChangesDecision actualDecision));
            Assert.Equal(expectedDecision, actualDecision);
        }

        private static void ExecuteDecisionCommand(UnsavedChangesDialogViewModel viewModel, UnsavedChangesDecision decision)
        {
            switch (decision)
            {
                case UnsavedChangesDecision.Save:
                    viewModel.SaveCommand.Execute();
                    return;

                case UnsavedChangesDecision.Discard:
                    viewModel.DiscardCommand.Execute();
                    return;

                case UnsavedChangesDecision.Cancel:
                    viewModel.CancelCommand.Execute();
                    return;

                default:
                    throw new ArgumentOutOfRangeException(nameof(decision), decision, "The unsaved-changes decision is not supported.");
            }
        }

        private static void SetRequestCloseListener(UnsavedChangesDialogViewModel viewModel, Action<IDialogResult> callback)
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            ArgumentNullException.ThrowIfNull(callback);

            ConstructorInfo constructor = typeof(DialogCloseListener).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, binder: null, [typeof(Action<IDialogResult>)], modifiers: null) ?? throw new MissingMethodException(typeof(DialogCloseListener).FullName, ".ctor(Action<IDialogResult>)");

            var listener = (DialogCloseListener)constructor.Invoke([callback]);

            FieldInfo backingField = typeof(UnsavedChangesDialogViewModel).GetField("<RequestClose>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingFieldException(typeof(UnsavedChangesDialogViewModel).FullName, "<RequestClose>k__BackingField");

            backingField.SetValue(viewModel, listener);
        }
    }
}