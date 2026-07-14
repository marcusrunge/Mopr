using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Bases
{
    // Internal base for modules; holds optional service references for derived types.
    internal abstract class CoreBase : ICoreBase, ICore
    {
        // Backing field for Imaging (assigned by derived modules).
        protected IImagingService? _imagingService;

        // Optional DICOM service for derived modules; may be null if DICOM functionality is not needed.
        private readonly IDicom? _dicom;

        // Lock object to synchronize access to the ExceptionThrown event handlers.
        private readonly object _exceptionThrownLock = new object();

        // Optional logger for derived modules; may be null if logging is not needed.
        private readonly ILogger? _logger;

        // Backing field for the ExceptionThrown event handlers.
        private Action<Exception>? _exceptionThrown;

        protected CoreBase(ILogger? logger, IDicom? dicom)
        {
            _logger = logger;
            _dicom = dicom;
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
        IDicom? ICoreBase.Dicom => _dicom;

        /// <inheritdoc/>
        public IImagingService? ImagingService => _imagingService;

        /// <inheritdoc/>
        ILogger? ICoreBase.Logger => _logger;

        /// <inheritdoc/>
        void ICoreBase.OnExceptionThrown(Exception exception)
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
    }
}