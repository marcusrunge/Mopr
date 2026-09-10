namespace MarcusRunge.Mopr.Workbench.Services.Miras.Contracts
{
    /// <summary>
    /// Defines the public contract of the assembly.
    /// </summary>
    public interface IMiras
    {
        /// <summary>
        /// Occurs when an exception is thrown.
        /// </summary>
        event Action<Exception> ExceptionThrown;

        /// <summary>
        /// Gets the IFlow instance exposed by the assembly, if available.
        /// </summary>
        IFlow? Flow { get; }

        /// <summary>
        /// Gets the IOperations instance exposed by the assembly, if available.
        /// </summary>
        IOperations? Operations { get; }
    }
}