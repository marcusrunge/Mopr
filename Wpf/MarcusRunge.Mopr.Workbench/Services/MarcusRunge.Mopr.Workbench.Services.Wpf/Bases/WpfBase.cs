using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts.Dialog;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts.Media;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MarcusRunge.Mopr.Workbench.Services.Wpf.Bases
{
    // Internal base for modules; holds optional service references for derived types.
    internal abstract class WpfBase(ILogger? logger) : IWpfBase, IWpf
    {
        // Backing field for IServiceA (assigned by derived modules).
        protected IDialogService? _dialogService;
        protected IMediaService? _mediaService;

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
        public IDialogService? DialogService => _dialogService;

        /// <inheritdoc/>
        ILogger? IWpfBase.Logger => logger;

        public IMediaService? MediaService => _mediaService;

        /// <inheritdoc/>
        void IWpfBase.OnExceptionThrown(Exception exception)
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