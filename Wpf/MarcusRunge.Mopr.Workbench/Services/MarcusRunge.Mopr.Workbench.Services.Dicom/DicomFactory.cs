using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using Microsoft.Extensions.Logging;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom
{

    public interface IDicomFactory
    {
        IDicom Create();
    }

    public class DicomFactory : IDicomFactory
    {
        private IDicom? _moduleInstance;

        private readonly ILogger? _logger;

        public DicomFactory()
        {
        }

        public DicomFactory(ILogger? logger)
        {
            _logger = logger;
        }


        /// <inheritdoc/>
        public IDicom Create() =>
            
            _moduleInstance ??= new Dicom.Implementations.Dicom(_logger);
    }
}
