using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts.Dialog;
using Microsoft.Win32;

namespace MarcusRunge.Mopr.Workbench.Services.Wpf.Implementations.Dialog
{
    internal class FileDialogService : CreateableBindableBase<IFileDialogService, FileDialogService, IDialogServiceBase>, IFileDialogService
    {
        public string? SelectFolder(string title, string? initialDirectory = null)
        {

            var dialog = new OpenFolderDialog
            {
                Title = title,
                Multiselect = false
            };

            if (!string.IsNullOrWhiteSpace(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }

            var result = dialog.ShowDialog();

            return result == true ? dialog.FolderName : null;

        }

        protected override void OnCreate(IDialogServiceBase @base)
        {
        }

        protected override Task OnCreateAsync(IDialogServiceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}