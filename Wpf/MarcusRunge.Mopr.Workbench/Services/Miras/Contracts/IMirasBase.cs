using MarcusRunge.Mopr.Workbench.Contracts.Application;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Contracts
{
    /// <summary>
    /// Internal base contract for exposing services to internal consumers.
    /// </summary>
    internal interface IMirasBase
    {
        /// <summary>
        /// Gets the application lifetime.
        /// </summary>
        internal IApplicationLifetime? ApplicationLifetime { get; }

        /// <summary>
        /// Gets the ILogger instance used for logging within the module.
        /// </summary>
        internal ILogger? Logger { get; }

        /// <summary>
        /// Gets the persistence for using within the module.
        /// </summary>
        internal IPersistence? Persistence { get; }

        /// <summary>
        /// Gets the repository.
        /// </summary>
        internal IRepository? Repository { get; }

        /// <summary>
        /// Called when [exception thrown].
        /// </summary>
        /// <param name="exception">The exception.</param>
        internal void OnExceptionThrown(Exception exception);
    }
}