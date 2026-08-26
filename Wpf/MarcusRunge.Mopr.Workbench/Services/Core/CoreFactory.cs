using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Core
{
    /// <summary>
    /// Defines a factory contract for creating a Core module instance.
    /// </summary>
    public interface ICoreFactory
    {
        /// <summary>
        /// Creates or returns the module instance owned by this factory.
        /// </summary>
        ICore Create();
    }

    /// <summary>
    /// Creates and retains one Core module instance per factory.
    /// </summary>
    public sealed class CoreFactory : ICoreFactory
    {
        private readonly IDicom? _dicom;
        private readonly ILogger? _logger;
        private ICore? _moduleInstance;

        public CoreFactory(IDicom? dicom) => _dicom = dicom;

        public CoreFactory(ILogger? logger, IDicom? dicom)
        {
            _logger = logger;
            _dicom = dicom;
        }

        /// <inheritdoc/>
        public ICore Create()
        {
            // The factory retains one stable Core instance with the dependencies supplied
            // to this specific factory. The composition root controls its overall lifetime.
            return _moduleInstance ??= new Implementations.Core(_logger, _dicom);
        }
    }
}