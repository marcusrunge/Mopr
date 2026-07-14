using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts.Media
{
    public interface IImageSourceService
    {
        bool CanLoadImageSource(string filePath);

        ImageSource? CreateImageSource(DicomGrayscaleImage image);

        ImageSource? LoadImageSource(string filePath);
    }
}