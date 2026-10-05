using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Contracts
{
    /// <summary>
    /// Internal base contract for exposing services to internal MIRAS consumers.
    /// </summary>
    internal interface IMirasBase
    {
        /// <summary>
        /// Gets the administrative authorization service.
        /// </summary>
        internal IAdministrativeAuthorizationService AdministrativeAuthorizationService { get; }

        /// <summary>
        /// Gets the application lifetime.
        /// </summary>
        internal ILifetimeService? LifetimeService { get; }

        /// <summary>
        /// Gets the logger used within the MIRAS module.
        /// </summary>
        internal ILogger? Logger { get; }

        /// <summary>
        /// Gets the MIRAS integrity operations.
        /// </summary>
        internal IOperations? Operations { get; }

        /// <summary>
        /// Gets the persistence module.
        /// </summary>
        internal IPersistence Persistence { get; }

        /// <summary>
        /// Gets the repository module.
        /// </summary>
        internal IRepository Repository { get; }

        /// <summary>
        /// Gets the repository-infrastructure recovery service.
        /// </summary>
        internal IRepositoryInfrastructureRecoveryService? RepositoryInfrastructureRecovery { get; }

        /// <summary>
        /// Gets the repository-location validation service.
        /// </summary>
        internal IRepositoryLocationValidationService RepositoryLocationValidationService { get; }

        /// <summary>
        /// Gets the technical audit-identity provider used for machine-wide infrastructure operations.
        /// </summary>
        internal ISystemAuditIdentityProvider SystemAuditIdentityProvider { get; }

        /// <summary>
        /// Reports an exception to the MIRAS composition boundary.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        internal void OnExceptionThrown(Exception exception);
    }
}