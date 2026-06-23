using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Bases
{
    internal abstract class DicomBase : IDicomBase, IDicom
    {
        protected IDicomImportService? _importService;
        protected IDicomMetadataService? _metadataService;
        private readonly object _exceptionThrownLock = new object();
        private readonly ILogger? _logger;
        private Action<Exception>? _exceptionThrown;

        protected DicomBase(ILogger? logger)
        {
            _logger = logger;
        }

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

        public IDicomImportService? ImportService => _importService;
        ILogger? IDicomBase.Logger => _logger;
        public IDicomMetadataService? MetadataService => _metadataService;

        void IDicomBase.OnExceptionThrown(Exception exception)
        {
            _logger?.LogError(exception, "Exception thrown in {AssemblyName}", Assembly.GetCallingAssembly().GetName().Name);
            Action<Exception>? handlers;
            lock (_exceptionThrownLock)
            {
                handlers = _exceptionThrown;
            }
            if (handlers is null)
                return;
            foreach (Action<Exception> handler in handlers.GetInvocationList().Cast<Action<Exception>>())
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