using MarcusRunge.Mopr.Workbench.Services.Persistence.Contexts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Bases
{
    // Internal base for modules; holds optional service references for derived types.
    internal abstract class PersistenceBase(ILogger? logger) : IPersistenceBase, IPersistence
    {
        // Backing field for IInstanceRepository (assigned by derived modules)
        protected IInstanceRepository? _instance;

        // Backing field for IMeasurementRepository (assigned by derived modules)
        protected IMeasurementRepository? _measurement;

        // Backing field for ISeriesRepository (assigned by derived modules)
        protected ISeriesRepository? _series;

        // Backing field for IStudyRepository (assigned by derived modules)
        protected IStudyRepository? _study;

        // Backing field for IUserRepository (assigned by derived modules)
        protected IUserRepository? _user;

        // Lock object to synchronize access to the ExceptionThrown event handlers.
        private readonly Lock _exceptionThrownLock = new();

        // Logger instance for logging within the module.
        private readonly ILogger? _logger = logger;

        // Backing field for the ExceptionThrown event handlers.
        private Action<Exception>? _exceptionThrown;

        /// <inheritdoc/>
        public event Action<Exception> ExceptionThrown
        {
            add
            {
                lock (_exceptionThrownLock) _exceptionThrown += value;
            }
            remove
            {
                lock (_exceptionThrownLock) _exceptionThrown -= value;
            }
        }

        /// <inheritdoc/>
        PersistenceConfiguration IPersistenceBase.Configuration => throw new NotImplementedException();

        /// <inheritdoc/>
        IDbContextFactory<PersistenceDbContext> IPersistenceBase.DbContextFactory => throw new NotImplementedException();

        /// <inheritdoc/>
        public IInstanceRepository? Instance => _instance;

        /// <inheritdoc/>
        ILogger? IPersistenceBase.Logger => _logger;

        /// <inheritdoc/>
        public IMeasurementRepository? Measurement => _measurement;

        /// <inheritdoc/>
        public ISeriesRepository? Series => _series;

        /// <inheritdoc/>
        public IStudyRepository? Study => _study;

        /// <inheritdoc/>
        public IUserRepository? User => _user;

        /// <inheritdoc/>
        Task IPersistenceBase.InitializeDatabaseAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        void IPersistenceBase.OnExceptionThrown(Exception exception)
        {
            // Log the exception with the module's logger, if available.
            _logger?.LogError(exception, "Exception thrown in {AssemblyName}", Assembly.GetCallingAssembly().GetName().Name);
            // Capture the current handlers to invoke outside the lock.
            Action<Exception>? handlers;
            // Lock to safely read the current handlers.
            lock (_exceptionThrownLock)
            {
                // Capture the current handlers to invoke outside the lock.
                handlers = _exceptionThrown;
            }
            // If there are no handlers, there's nothing to invoke.
            if (handlers is null)
                return;
            // Invoke each handler in a try-catch to ensure one failing handler doesn't prevent others from being notified.
            foreach (Action<Exception> handler in handlers.GetInvocationList().Cast<Action<Exception>>())
            {
                try
                {
                    // Invoke the handler with the exception.
                    handler(exception);
                }
                catch (Exception callbackException)
                {
                    // Log any exceptions thrown by the handlers, but continue invoking the remaining handlers.
                    _logger?.LogError(callbackException, "Exception thrown by ExceptionThrown event handler.");
                }
            }
        }

        /// <inheritdoc/>
        Task<bool> IPersistenceBase.TestConnectionAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}