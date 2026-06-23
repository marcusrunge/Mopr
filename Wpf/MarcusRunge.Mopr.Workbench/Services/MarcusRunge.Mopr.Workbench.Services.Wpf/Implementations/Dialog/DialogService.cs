using MarcusRunge.Mopr.Workbench.Services.Wpf.Bases;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts.Dialog;

namespace MarcusRunge.Mopr.Workbench.Services.Wpf.Implementations.Dialog
{
    internal class DialogService : DialogServiceBase
    {
        internal DialogService(IWpfBase? wpfBase) : base(wpfBase)
        {
            _fileDialogService = Dialog.FileDialogService.Create(this);
        }

        internal static IDialogService? Create(IWpfBase? wpfBase) => wpfBase is null ? null : new DialogService(wpfBase);
    }
}