using MarcusRunge.Mopr.Workbench.Contracts.Application;
using MarcusRunge.Mopr.Workbench.Services.Miras.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Bases
{
    // Internal base for modules; holds optional service references for derived types.
    internal abstract class MirasBase(ILogger? logger, IApplicationLifetime? applicationLifetime, IPersistence persistence, IRepository repository) : IMirasBase, IMiras
    {
        // Backing field for IServiceA (assigned by derived modules).
        protected IMirasService? _mirasService;

        // Lock object to synchronize access to the ExceptionThrown event handlers.
        private readonly Lock _exceptionThrownLock = new();

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
        IApplicationLifetime? IMirasBase.ApplicationLifetime => applicationLifetime;

        /// <inheritdoc/>
        ILogger? IMirasBase.Logger => logger;

        /// <inheritdoc/>
        public IMirasService? MirasService => _mirasService;

        /// <inheritdoc/>
        IPersistence? IMirasBase.Persistence => persistence;

        /// <inheritdoc/>
        IRepository? IMirasBase.Repository => repository;

        /// <inheritdoc/>
        void IMirasBase.OnExceptionThrown(Exception exception)
        {
            // Log the exception with the module's logger, if available.
            logger?.LogError(exception, "Exception thrown in {AssemblyName}", Assembly.GetCallingAssembly().GetName().Name);
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
                    logger?.LogError(callbackException, "Exception thrown by ExceptionThrown event handler.");
                }
            }
        }
    }
}