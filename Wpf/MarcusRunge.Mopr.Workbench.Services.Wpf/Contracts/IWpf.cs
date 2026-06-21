namespace MarcusRunge.Mopr.Workbench.Services.Contracts
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
        /// Gets the IServiceA instance exposed by the assembly, if available.
        /// </summary>
        IServiceA? ServiceA { get; }

        /// <summary>
        /// Gets the IServiceB instance exposed by the assembly, if available.
        /// </summary>
        IServiceB? ServiceB { get; }
    }
}