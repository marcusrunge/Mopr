using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Bases
{
    internal abstract class DialogServiceBase(IApplicationBase? applicationBase) : IDialogServiceBase, IDialogService
    {
        protected IFileDialogService? _fileDialogService;

        /// <inheritdoc/>
        public IFileDialogService? FileDialogService => _fileDialogService;

        IApplicationBase? IServiceBase.ApplicationBase => applicationBase;
    }
}