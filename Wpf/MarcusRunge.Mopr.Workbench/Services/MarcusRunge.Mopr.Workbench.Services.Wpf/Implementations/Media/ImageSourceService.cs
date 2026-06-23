using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts.Media;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MarcusRunge.Mopr.Workbench.Services.Wpf.Implementations.Media
{
    internal class ImageSourceService : CreateableBindableBase<IImageSourceService, ImageSourceService, IMediaServiceBase>, IImageSourceService
    {
        public bool CanLoadImageSource(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            var extension = Path.GetExtension(filePath);

            return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".tif", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".tiff", StringComparison.OrdinalIgnoreCase);
        }

        public ImageSource? LoadImageSource(string filePath)
        {
            if (!CanLoadImageSource(filePath))
            {
                return null;
            }

            try
            {
                var bitmapImage = new BitmapImage();

                using (var stream = File.OpenRead(filePath))
                {
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                }

                return bitmapImage;
            }
            catch
            {
                return null;
            }
        }

        protected override void OnCreate(IMediaServiceBase @base)
        {
        }

        protected override Task OnCreateAsync(IMediaServiceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}