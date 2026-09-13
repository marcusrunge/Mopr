namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog
{
    /// <summary>
    /// Provides the public application use case for managing dialogs.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Gets the file dialog service.
        /// </summary>
        IFileDialogService? FileDialogService { get; }
    }
}