namespace MarcusRunge.Mopr.Workbench.Services.Interfaces.Dialog
{
    public interface IFileDialogService
    {
        string? SelectFolder(string title, string? initialDirectory = null);
    }
}