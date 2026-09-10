namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog
{
    public interface IFileDialogService
    {
        string? SelectFolder(string title, string? initialDirectory = null);
    }
}