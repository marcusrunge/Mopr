namespace MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts
{
    /// <summary>
    /// Defines the public contract of the assembly.
    /// </summary>
    public interface IWpf
    {
        /// <summary>
        /// Occurs when an exception is thrown.
        /// </summary>
        event Action<Exception> ExceptionThrown;

        /// <summary>
        /// Gets the IDialogService instance exposed by the assembly, if available.
        /// </summary>
        IDialogService? DialogService { get; }
    }
}