using MarcusRunge.Mopr.Workbench.Contracts.Miras;

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
        /// Gets the IMirasService instance exposed by the assembly, if available.
        /// </summary>
        IMirasService? MirasService { get; }
    }
}