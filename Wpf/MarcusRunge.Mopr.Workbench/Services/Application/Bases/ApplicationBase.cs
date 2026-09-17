using MarcusRunge.Mopr.Workbench.Contracts.Application.Identity.Services;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Security.Services;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Dialog;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Identity;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Media;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Bases
{
    /// <summary>
    /// Provides shared dependencies and service references for one application-service graph.
    /// </summary>
    internal abstract class ApplicationBase(ILogger? logger, IOperatingSystemIdentityProvider? operatingSystemIdentityProvider, IPersistence? persistence, IRepository? repository) : IApplicationBase, IApplication
    {
        protected IDialogService? _dialogService;
        protected IIdentityService? _identityService;
        protected IImportService? _importService;
        protected IMediaService? _mediaService;

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
        IAuditIdentityProvider? IApplicationBase.AuditIdentityProvider => (_identityService as IIdentityServiceBase)?.AuditIdentityProvider;

        /// <inheritdoc/>
        public IDialogService? DialogService => _dialogService;

        /// <inheritdoc/>
        public IIdentityService? IdentityService => _identityService;

        /// <inheritdoc/>
        public IImportService? ImportService => _importService;

        /// <inheritdoc/>
        ILogger? IApplicationBase.Logger => logger;

        /// <inheritdoc/>
        public IMediaService? MediaService => _mediaService;

        /// <inheritdoc/>
        IOperatingSystemIdentityProvider? IApplicationBase.OperatingSystemIdentityProvider => operatingSystemIdentityProvider;

        /// <inheritdoc/>
        IPersistence? IApplicationBase.Persistence => persistence;

        /// <inheritdoc/>
        IRepository? IApplicationBase.Repository => repository;

        /// <inheritdoc/>
        void IApplicationBase.OnExceptionThrown(Exception exception)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            logger?.LogError(exception, "Exception thrown in {AssemblyName}", Assembly.GetCallingAssembly().GetName().Name);

            Action<Exception>? handlers;

            // Handlers are captured under the lock and invoked afterward so a
            // callback cannot block subscription changes or cause a deadlock.
            lock (_exceptionThrownLock)
            {
                handlers = _exceptionThrown;
            }

            if (handlers is null)
            {
                return;
            }

            foreach (Action<Exception> handler in handlers.GetInvocationList().Cast<Action<Exception>>())
            {
                try
                {
                    handler(exception);
                }
                catch (Exception callbackException)
                {
                    // One failing subscriber must not prevent the remaining
                    // application exception observers from being notified.
                    logger?.LogError(callbackException, "Exception thrown by ExceptionThrown event handler.");
                }
            }
        }
    }
}