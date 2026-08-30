using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Contracts.Miras;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Bases
{
    /// <summary>
    /// Provides the shared dependencies and service references of one Core module instance.
    /// </summary>
    internal abstract class CoreBase(ILogger? logger, IDicom? dicom, IApplicationLifetime applicationLifetime, IMirasService mirasCheckService) : ICoreBase, ICore
    {
        protected IImagingService? _imagingService;
        protected IMirasApplicationService? _mirasApplicationService;

        private readonly IApplicationLifetime _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        private readonly IDicom? _dicom = dicom;
        private readonly object _exceptionThrownLock = new();
        private readonly ILogger? _logger = logger;
        private readonly IMirasService _mirasService = mirasCheckService ?? throw new ArgumentNullException(nameof(mirasCheckService));

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
        IApplicationLifetime ICoreBase.ApplicationLifetime => _applicationLifetime;

        /// <inheritdoc/>
        IDicom? ICoreBase.Dicom => _dicom;

        /// <inheritdoc/>
        public IImagingService? ImagingService => _imagingService;

        /// <inheritdoc/>
        ILogger? ICoreBase.Logger => _logger;

        /// <inheritdoc/>
        public IMirasApplicationService? MirasApplicationService => _mirasApplicationService;

        /// <inheritdoc/>
        IMirasService ICoreBase.MirasService => _mirasService;

        /// <inheritdoc/>
        void ICoreBase.OnExceptionThrown(Exception exception)
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

            // Subscriber failures are isolated so every registered diagnostics
            // consumer receives the original Core exception.
            foreach (var handler in handlers.GetInvocationList().Cast<Action<Exception>>())
            {
                try
                {
                    handler(exception);
                }
                catch (Exception callbackException)
                {
                    _logger?.LogError(callbackException, "Exception thrown by ExceptionThrown event handler.");
                }
            }
        }
    }
}