using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Contracts.Miras;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using Microsoft.Extensions.Logging;
using System;

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
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly IDicom? _dicom;
        private readonly ILogger? _logger;
        private readonly IMirasService _mirasService;

        private ICore? _moduleInstance;

        public CoreFactory(IDicom? dicom, IApplicationLifetime applicationLifetime, IMirasService mirasService)
        {
            _dicom = dicom;
            _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
            _mirasService = mirasService ?? throw new ArgumentNullException(nameof(mirasService));
        }

        public CoreFactory(ILogger? logger, IDicom? dicom, IApplicationLifetime applicationLifetime, IMirasService mirasService)
        {
            _logger = logger;
            _dicom = dicom;
            _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
            _mirasService = mirasService ?? throw new ArgumentNullException(nameof(mirasService));
        }

        /// <inheritdoc/>
        public ICore Create()
        {
            // The factory retains one stable Core instance with all dependencies
            // supplied by the composition root.
            return _moduleInstance ??= new Implementations.Core(_logger, _dicom, _applicationLifetime, _mirasService);
        }
    }
}