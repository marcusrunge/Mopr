using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Bases
{
    // Internal base for modules; holds optional service references for derived types.
    internal abstract class RepositoryBase : IRepositoryBase, IRepository
    {
        protected IDicomImportService? _importService;
        protected IRepositoryOperationsCoordinator? _operationsCoordinator;
        protected IDicomRepositoryRepairService? _repositoryRepairService;
        protected IDicomRepositoryService? _repositoryService;

        private readonly IDisposable? _applicationConfigurationSubscription;
        private readonly IApplicationLifetime? _applicationLifetime;
        private readonly Lock _exceptionThrownLock = new();
        private readonly ILogger? _logger;
        private readonly IPersistence? _persistence;
        private readonly CancellationTokenRegistration _shutdownRegistration;

        private IApplicationConfiguration? _applicationConfiguration;
        private Action<Exception>? _exceptionThrown;

        internal RepositoryBase(ILogger? logger, IApplicationLifetime? applicationLifetime, IObservable<IApplicationConfiguration>? applicationConfigurationObservable, IPersistence? persistence)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _persistence = persistence;

            if (applicationLifetime is null)
            {
                throw new ArgumentNullException(nameof(applicationLifetime), "Application lifetime cannot be null.");
            }

            _shutdownRegistration = applicationLifetime.ApplicationStopping.Register(() =>
            {
                try
                {
                    _applicationConfigurationSubscription?.Dispose();
                }
                catch
                {
                    /*
                     * Application shutdown must continue even if an observable
                     * subscription fails during disposal.
                     */
                }
            });

            _applicationConfigurationSubscription = applicationConfigurationObservable?.Subscribe(configuration =>
            {
                _ = HandleConfigurationChangedAsync(configuration);
            });
        }

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
        IApplicationConfiguration? IRepositoryBase.ApplicationConfiguration => _applicationConfiguration;

        /// <inheritdoc/>
        public IDicomImportService? ImportService => _importService;

        /// <inheritdoc/>
        ILogger? IRepositoryBase.Logger => _logger;

        /// <inheritdoc/>
        IRepositoryOperationsCoordinator? IRepositoryBase.OperationsCoordinator => _operationsCoordinator;

        /// <inheritdoc/>
        IPersistence? IRepositoryBase.Persistence => _persistence;

        /// <inheritdoc/>
        public IDicomRepositoryRepairService? RepositoryRepairService => _repositoryRepairService;

        /// <inheritdoc/>
        public IDicomRepositoryService? RepositoryService => _repositoryService;

        /// <inheritdoc/>
        void IRepositoryBase.OnExceptionThrown(Exception exception)
        {
            _logger?.LogError(exception, "Exception thrown in {AssemblyName}", Assembly.GetCallingAssembly().GetName().Name);

            Action<Exception>? handlers;

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
                    /*
                     * One failing observer must not prevent the remaining exception
                     * observers from receiving the original repository failure.
                     */
                    _logger?.LogError(callbackException, "Exception thrown by ExceptionThrown event handler.");
                }
            }
        }

        private Task HandleConfigurationChangedAsync(IApplicationConfiguration configuration)
        {
            try
            {
                _applicationConfiguration = configuration;
            }
            catch (Exception exception)
            {
                ((IRepositoryBase)this).OnExceptionThrown(exception);
            }

            return Task.CompletedTask;
        }
    }
}