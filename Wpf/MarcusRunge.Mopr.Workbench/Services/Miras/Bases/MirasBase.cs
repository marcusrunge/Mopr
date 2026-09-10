using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
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
    internal abstract class MirasBase(ILogger? logger, ILifetimeService? applicationLifetime, IPersistence persistence, IRepository repository) : IMirasBase, IMiras
    {
        protected IFlow? _flow;
        protected IOperations? _operations;

        private readonly Lock _exceptionThrownLock = new();
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
        ILifetimeService? IMirasBase.ApplicationLifetime => applicationLifetime;

        public IFlow? Flow => _flow;

        /// <inheritdoc/>
        ILogger? IMirasBase.Logger => logger;

        /// <inheritdoc/>
        public IOperations? Operations => _operations;

        /// <inheritdoc/>
        IPersistence? IMirasBase.Persistence => persistence;

        /// <inheritdoc/>
        IRepository? IMirasBase.Repository => repository;

        /// <inheritdoc/>
        void IMirasBase.OnExceptionThrown(Exception exception)
        {
            logger?.LogError(exception, "Exception thrown in {AssemblyName}", Assembly.GetCallingAssembly().GetName().Name);

            // Capture the immutable invocation snapshot under the lock so handlers can be invoked without blocking event subscription changes.
            Action<Exception>? handlers;
            lock (_exceptionThrownLock) handlers = _exceptionThrown;

            if (handlers is null)
            {
                return;
            }

            // Isolate event subscribers so one failing diagnostic callback cannot suppress notification of the remaining subscribers.
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