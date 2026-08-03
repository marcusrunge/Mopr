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
        /// Gets the logger used within the module.
        /// </summary>
        internal ILogger? Logger { get; }

        /// <summary>
        /// Gets the central operations coordinator shared by import and repair.
        /// </summary>
        internal IRepositoryOperationsCoordinator? OperationsCoordinator { get; }

        /// <summary>
        /// Gets the Persistence module used within the repository module.
        /// </summary>
        internal IPersistence? Persistence { get; }

        /// <summary>
        /// Reports an exception through the repository module.
        /// </summary>
        /// <param name="exception">
        /// The exception.
        /// </param>
        internal void OnExceptionThrown(Exception exception);
    }
}