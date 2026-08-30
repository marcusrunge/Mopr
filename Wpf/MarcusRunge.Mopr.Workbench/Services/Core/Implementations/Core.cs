using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime;
using MarcusRunge.Mopr.Workbench.Contracts.Miras;
using MarcusRunge.Mopr.Workbench.Services.Core.Bases;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Implementations
{
    /// <summary>
    /// Composes the services owned by one Core module instance.
    /// </summary>
    internal sealed class Core : CoreBase
    {
        internal Core(ILogger? logger, IDicom? dicom, IApplicationLifetime applicationLifetime, IMirasService mirasService) : base(logger, dicom, applicationLifetime, mirasService)
        {
            // Imaging remains independent of Persistence and MIRAS, while the
            // application service receives the shared Core lifetime and check port.
            _imagingService = Implementations.ImagingService.Create(this);
            _mirasApplicationService = Implementations.MirasApplicationService.Create(this);
        }
    }
}