using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts
{
    /// <summary>
    /// Internal base contract for exposing services to internal consumers.
    /// </summary>
    internal interface IPersistenceBase
    {
        /// <summary>
        /// Gets the application lifetime.
        /// </summary>
        internal IApplicationLifetime? ApplicationLifetime { get; }

        /// <summary>
        /// Gets the persistence configuration.
        /// </summary>
        internal PersistenceConfiguration? Configuration { get; }

        /// <summary>
        /// Gets the ILogger instance used for logging within the module.
        /// </summary>
        internal ILogger? Logger { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="PersistenceDbContext"/> class.
        /// </summary>
        /// <returns>The created DbContext instance.</returns>
        internal PersistenceDbContext CreateDbContext();

        /// <summary>
        /// Initializes the database.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        internal Task InitializeDatabaseAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Called when [exception thrown].
        /// </summary>
        /// <param name="exception">The exception.</param>
        internal void OnExceptionThrown(Exception exception);

        /// <summary>
        /// Tests the connection to the database.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        internal Task<PersistenceConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken);
    }
}