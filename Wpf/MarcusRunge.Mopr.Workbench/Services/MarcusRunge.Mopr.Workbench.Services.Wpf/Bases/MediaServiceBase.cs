using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Wpf.Contracts.Media;

namespace MarcusRunge.Mopr.Workbench.Services.Wpf.Bases
{
    internal abstract class MediaServiceBase(IWpfBase? wpfBase) : IMediaServiceBase, IMediaService
    {
        protected IImageSourceService? _imageSourceService;
        public IImageSourceService? ImageSourceService => _imageSourceService;

        IWpfBase? IServiceBase.WpfBase => wpfBase;
    }
}