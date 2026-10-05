using MarcusRunge.Mopr.Workbench.Contracts.Application.Administration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Bases
{
    /// <summary>
    /// Provides the dependencies and exception propagation shared by one MIRAS module instance.
    /// </summary>
    internal abstract class MirasBase(ILogger? logger, ILifetimeService? applicationLifetime, IPersistence persistence, IRepository repository, IAdministrativeAuthorizationService administrativeAuthorizationService, IRepositoryLocationValidationService repositoryLocationValidationService, ISystemAuditIdentityProvider systemAuditIdentityProvider) : IMirasBase, IMiras
    {
        protected IFlow? _flow;
        protected IOperations? _operations;
        protected IRepositoryInfrastructureRecoveryService? _repositoryInfrastructureRecovery;

        private readonly Lock _exceptionThrownLock = new();
        private Action<Exception>? _exceptionThrown;

        /// <inheritdoc/>
        public event Action<Exception> ExceptionThrown
        {
            add
            {
                lock (_exceptionThrownLock)
                {
                    _exceptionThrown += value;
                }
            }
            remove
            {
                lock (_exceptionThrownLock)
                {
                    _exceptionThrown -= value;
                }
            }
        }

        /// <inheritdoc/>
        IAdministrativeAuthorizationService IMirasBase.AdministrativeAuthorizationService => administrativeAuthorizationService;

        /// <inheritdoc/>
        ILifetimeService? IMirasBase.LifetimeService => applicationLifetime;

        /// <inheritdoc/>
        public IFlow? Flow => _flow;

        /// <inheritdoc/>
        ILogger? IMirasBase.Logger => logger;

        /// <inheritdoc/>
        public IOperations? Operations => _operations;

        /// <inheritdoc/>
        IPersistence IMirasBase.Persistence => persistence;

        /// <inheritdoc/>
        IRepository IMirasBase.Repository => repository;

        /// <inheritdoc/>
        public IRepositoryInfrastructureRecoveryService? RepositoryInfrastructureRecovery => _repositoryInfrastructureRecovery;

        /// <inheritdoc/>
        IRepositoryLocationValidationService IMirasBase.RepositoryLocationValidationService => repositoryLocationValidationService;

        /// <inheritdoc/>
        ISystemAuditIdentityProvider IMirasBase.SystemAuditIdentityProvider => systemAuditIdentityProvider;

        /// <inheritdoc/>
        void IMirasBase.OnExceptionThrown(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            logger?.LogError(exception, "Exception thrown in {AssemblyName}", Assembly.GetCallingAssembly().GetName().Name);

            Action<Exception>? handlers;

            // Capture an immutable invocation snapshot so subscriber changes do not
            // block or modify the current diagnostic notification.
            lock (_exceptionThrownLock)
            {
                handlers = _exceptionThrown;
            }

            if (handlers is null)
            {
                return;
            }

            // A failing diagnostic subscriber must never suppress notification of
            // the remaining subscribers or alter the MIRAS operation result.
            foreach (var handler in handlers.GetInvocationList().Cast<Action<Exception>>())
            {
                try
                {
                    handler(exception);
                }
                catch (Exception callbackException)
                {
                    logger?.LogError(callbackException, "Exception thrown by an ExceptionThrown event handler.");
                }
            }
        }
    }
}