using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using Microsoft.Extensions.Logging;
using RepositoryContract = MarcusRunge.Mopr.Workbench.Services.Repository.Contracts.IRepository;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts
{
    /// <summary>
    /// Internal base contract for exposing services to internal consumers.
    /// </summary>
    internal interface IApplicationBase
    {
        /// <summary>
        /// Gets the IAuditIdentityProvider instance used for audit identity operations within the module.
        /// </summary>
        internal IAuditIdentityProvider? AuditIdentityProvider { get; }

        /// <summary>
        /// Gets the ILogger instance used for logging within the module.
        /// </summary>
        internal ILogger? Logger { get; }

        /// <summary>
        /// Gets the IPersistence instance used for persistence operations within the module.
        /// </summary>
        internal IPersistence? Persistence { get; }

        /// <summary>
        /// Gets the RepositoryContract instance used for repository operations within the module.
        /// </summary>
        internal RepositoryContract? Repository { get; }

        /// <summary>
        /// Called when [exception thrown].
        /// </summary>
        /// <param name="exception">The exception.</param>
        internal void OnExceptionThrown(Exception exception);
    }
}