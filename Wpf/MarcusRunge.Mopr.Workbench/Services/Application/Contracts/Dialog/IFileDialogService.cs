namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog
{
    /// <summary>
    /// Provides the public application use case for managing file dialogs.
    /// </summary>
    public interface IFileDialogService
    {
        /// <summary>
        /// Selects a folder.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="initialDirectory">The initial directory.</param>
        /// <returns>The selected folder path or null if canceled.</returns>
        string? SelectFolder(string title, string? initialDirectory = null);
    }
}