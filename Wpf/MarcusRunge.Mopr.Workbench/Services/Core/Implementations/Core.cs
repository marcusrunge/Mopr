using MarcusRunge.Mopr.Workbench.Contracts.Application.Lifetime.Services;
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
        internal Core(ILogger? logger, IDicom? dicom, ILifetimeService applicationLifetime) : base(logger, dicom, applicationLifetime) => _imagingService = Implementations.ImagingService.Create(this);
    }
}