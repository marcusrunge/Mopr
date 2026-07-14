namespace MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts.Dialog
{
    public interface IFileDialogService
    {
        string? SelectFolder(string title, string? initialDirectory = null);
    }
}