using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom
{
    /// <summary>
    /// Defines a factory contract for creating a DICOM module instance.
    /// </summary>
    public interface IDicomFactory
    {
        /// <summary>
        /// Creates or returns the module instance owned by this factory.
        /// </summary>
        IDicom Create();
    }

    /// <summary>
    /// Creates and retains one DICOM module instance per factory.
    /// </summary>
    public sealed class DicomFactory : IDicomFactory
    {
        private readonly ILogger? _logger;
        private IDicom? _moduleInstance;

        public DicomFactory()
        {
        }

        public DicomFactory(ILogger? logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public IDicom Create()
        {
            // The factory retains one stable DICOM instance. The composition root controls
            // the overall lifetime through the factory registration.
            return _moduleInstance ??= new Implementations.Dicom(_logger);
        }
    }
}