using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts
{
    public interface IDicomImageService
    {
        Task<int> GetFrameCountAsync(string filePath, CancellationToken cancellationToken = default);
        Task<DicomGrayscaleImage?> LoadGrayscaleImageAsync(string filePath, double? windowCenter = null, double? windowWidth = null, CancellationToken cancellationToken = default);
        Task<DicomGrayscaleImage?> LoadGrayscaleImageAsync(string filePath, int frameIndex, double? windowCenter = null, double? windowWidth = null, CancellationToken cancellationToken = default);
        Task<DicomImageFrame?> LoadImageFrameAsync(string filePath, CancellationToken cancellationToken = default);
        Task<DicomImageFrame?> LoadImageFrameAsync(string filePath, int frameIndex, CancellationToken cancellationToken = default);
        DicomGrayscaleImage? RenderGrayscaleImage(DicomImageFrame frame, double? windowCenter = null, double? windowWidth = null);
    }
}
