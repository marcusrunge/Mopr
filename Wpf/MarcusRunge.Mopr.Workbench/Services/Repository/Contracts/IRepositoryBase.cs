using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Contracts
{
    /// <summary>
    /// Internal base contract for exposing services to internal consumers.
    /// </summary>
    internal interface IRepositoryBase
    {
        /// <summary>
        /// Gets the application configuration.
        /// </summary>
        internal IApplicationConfiguration? ApplicationConfiguration { get; }

        /// <summary>
        /// Gets the ILogger instance used for logging within the module.
        /// </summary>
        internal ILogger? Logger { get; }

        /// <summary>
        /// Gets the persistence for using within the module.
        /// </summary>
        internal IPersistence? Persistence { get; }

        /// <summary>
        /// Called when [exception thrown].
        /// </summary>
        /// <param name="exception">The exception.</param>
        internal void OnExceptionThrown(Exception exception);
    }
}