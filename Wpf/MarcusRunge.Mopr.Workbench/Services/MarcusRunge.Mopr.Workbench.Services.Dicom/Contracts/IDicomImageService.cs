using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts
{
    public interface IDicomImageService
    {
        Task<DicomGrayscaleImage?> LoadGrayscaleImageAsync(string filePath, CancellationToken cancellationToken = default);
    }
}