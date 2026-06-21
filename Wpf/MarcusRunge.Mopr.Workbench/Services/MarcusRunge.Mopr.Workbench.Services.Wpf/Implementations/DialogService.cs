using MarcusRunge.Mopr.Workbench.Services.Wpf.Bases;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;

namespace MarcusRunge.Mopr.Workbench.Services.Wpf.Implementations
{
    internal class DialogService : DialogServiceBase
    {
        public DialogService(IWpfBase? wpfBase) : base(wpfBase)
        {
            _fileDialogService = Dialog.FileDialogService.Create(this);
        }

        internal static IDialogService? Create(IWpfBase? wpfBase) => wpfBase is null ? null : new DialogService(wpfBase);
    }
}