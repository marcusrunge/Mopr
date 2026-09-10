using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Media
{
    public interface IImageSourceService
    {
        bool CanLoadImageSource(string filePath);

        ImageSource? CreateImageSource(DicomGrayscaleImage image);

        ImageSource? LoadImageSource(string filePath);
    }
}