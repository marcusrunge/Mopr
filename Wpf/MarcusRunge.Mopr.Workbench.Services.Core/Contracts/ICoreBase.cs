using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts
{
    /// <summary>
    /// Internal base contract for exposing services to internal consumers.
    /// </summary>
    internal interface ICoreBase
    {
        /// <summary>
        /// Gets the ILogger instance used for logging within the module.
        /// </summary>
        internal ILogger? Logger { get; }

        /// <summary>
        /// Called when [exception thrown].
        /// </summary>
        /// <param name="exception">The exception.</param>
        internal void OnExceptionThrown(Exception exception);
    }
}