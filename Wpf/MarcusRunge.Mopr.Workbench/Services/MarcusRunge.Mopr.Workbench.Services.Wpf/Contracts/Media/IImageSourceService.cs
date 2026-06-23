using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts.Media
{
    public interface IImageSourceService
    {
        bool CanLoadImageSource(string filePath);

        ImageSource? LoadImageSource(string filePath);
    }
}