using MarcusRunge.Mopr.Workbench.Services.Dicom.Bases;
using Microsoft.Extensions.Logging;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Implementations
{
    internal sealed class Dicom : DicomBase
    {
        internal Dicom(ILogger? logger) : base(logger)
        {
            _metadataService = DicomMetadataService.Create(this);
            _importService = DicomImportService.Create(this);
            _imageService = DicomImageService.Create(this);
        }
    }
}