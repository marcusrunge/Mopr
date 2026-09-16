using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
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
        /// Gets the audit identity provider used for audit identity operations within the application-service graph.
        /// </summary>
        internal IAuditIdentityProvider? AuditIdentityProvider { get; }

        /// <summary>
        /// Gets the logger used within the application-service graph.
        /// </summary>
        internal ILogger? Logger { get; }

        /// <summary>
        /// Gets the operating-system identity provider used to resolve the current authenticated identity.
        /// </summary>
        internal IOperatingSystemIdentityProvider? OperatingSystemIdentityProvider { get; }

        /// <summary>
        /// Gets the Persistence instance used within the application-service graph.
        /// </summary>
        internal IPersistence? Persistence { get; }

        /// <summary>
        /// Gets the Repository instance used within the application-service graph.
        /// </summary>
        internal RepositoryContract? Repository { get; }

        /// <summary>
        /// Reports an exception raised within the application-service graph.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        internal void OnExceptionThrown(Exception exception);
    }
}