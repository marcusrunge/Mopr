using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts.Dialog;

namespace MarcusRunge.Mopr.Workbench.Services.Wpf.Bases
{
    internal abstract class DialogServiceBase(IWpfBase? wpfBase) : IDialogServiceBase, IDialogService
    {
        protected IFileDialogService? _fileDialogService;
        private readonly IWpfBase? _wpfBase;

        public IFileDialogService? FileDialogService => _fileDialogService;

        IWpfBase? IDialogServiceBase.WpfBase => wpfBase;
    }
}