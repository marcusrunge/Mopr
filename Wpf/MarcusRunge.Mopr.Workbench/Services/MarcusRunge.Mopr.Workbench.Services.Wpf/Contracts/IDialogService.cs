using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts.Dialog;

namespace MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts
{
    public interface IDialogService
    {
        IFileDialogService? FileDialogService { get; }
    }
}