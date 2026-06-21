using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Bases
{
    // Internal base for modules; holds optional service references for derived types.
    internal abstract class CoreBase : ICoreBase, ICore
    {
        // Backing field for Imaging (assigned by derived modules).
        protected IImagingService? _imaging;

        // Lock object to synchronize access to the ExceptionThrown event handlers.
        private readonly object _exceptionThrownLock = new object();

        // Backing field for the ExceptionThrown event handlers.
        private Action<Exception>? _exceptionThrown;

        protected CoreBase(ILogger? logger)
        {
        }

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
        public IImagingService? Imaging => _imaging;

        /// <inheritdoc/>
        ILogger? ICoreBase.Logger => logger;

        /// <inheritdoc/>
        void ICoreBase.OnExceptionThrown(Exception exception)
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