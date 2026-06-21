using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts.Dialog;

namespace MarcusRunge.Mopr.Workbench.Services.Wpf.Implementations.Dialog
{
    internal class FileDialogService : CreateableBindableBase<IFileDialogService, FileDialogService, IDialogServiceBase>, IFileDialogService
    {
        public string? SelectFolder(string title, string? initialDirectory = null)
        {
            throw new NotImplementedException();
        }

        protected override void OnCreate(IDialogServiceBase @base)
        {
        }

        protected override Task OnCreateAsync(IDialogServiceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}