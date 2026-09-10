using MarcusRunge.Mopr.Workbench.Services.Application.Bases;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Dialog
{
    internal class DialogService : DialogServiceBase
    {
        internal DialogService(IApplicationBase? applicationBase) : base(applicationBase) => _fileDialogService = Dialog.FileDialogService.Create(this);

        internal static IDialogService? Create(IApplicationBase? applicationBase) => applicationBase is null ? null : new DialogService(applicationBase);
    }
}