using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Contracts
{
    /// <summary>
    /// Internal base contract for exposing services to internal consumers.
    /// </summary>
    internal interface IWpfBase
    {
        /// <summary>
        /// Gets the ILogger instance used for logging within the module.
        /// </summary>
        internal ILogger? Logger { get; }

        /// <summary>
        /// Gets the IServiceI instance used for internal assembly operations.
        /// </summary>
        IServiceI? ServiceI { get; }

        /// <summary>
        /// Called when [exception thrown].
        /// </summary>
        /// <param name="exception">The exception.</param>
        internal void OnExceptionThrown(Exception exception);
    }
}