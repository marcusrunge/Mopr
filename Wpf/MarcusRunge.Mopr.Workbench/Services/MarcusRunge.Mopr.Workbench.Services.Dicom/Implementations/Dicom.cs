using MarcusRunge.Mopr.Workbench.Services.Dicom.Bases;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Implementations
{
    internal sealed class Dicom : DicomBase
    {
        internal Dicom(ILogger? logger) : base(logger)
        {
            _metadataService = Implementations.DicomMetadataService.Create(this);
            _importService = Implementations.DicomImportService.Create(this);            
        }
    }
}