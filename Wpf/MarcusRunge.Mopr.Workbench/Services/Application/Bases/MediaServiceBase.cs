using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Media;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Bases
{
    internal abstract class MediaServiceBase(IApplicationBase? applicationBase) : IMediaServiceBase, IMediaService
    {
        protected IImageSourceService? _imageSourceService;
        public IImageSourceService? ImageSourceService => _imageSourceService;

        IApplicationBase? IServiceBase.ApplicationBase => applicationBase;
    }
}