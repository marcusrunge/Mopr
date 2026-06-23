using MarcusRunge.Mopr.Workbench.Services.Wpf.Bases;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts.Media;

namespace MarcusRunge.Mopr.Workbench.Services.Wpf.Implementations.Media
{
    internal class MediaService : MediaServiceBase
    {
        internal MediaService(IWpfBase? wpfBase) : base(wpfBase)
        {
            _imageSourceService = Media.ImageSourceService.Create(this);
        }

        internal static IMediaService? Create(IWpfBase? wpfBase) => wpfBase is null ? null : new MediaService(wpfBase);
    }
}