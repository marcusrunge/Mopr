using MarcusRunge.Mopr.Workbench.Services.Application.Bases;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Media;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Implementations.Media
{
    internal class MediaService : MediaServiceBase
    {
        internal MediaService(IApplicationBase? applicationBase) : base(applicationBase) => _imageSourceService = Media.ImageSourceService.Create(this);

        internal static IMediaService? Create(IApplicationBase? applicationBase) => applicationBase is null ? null : new MediaService(applicationBase);
    }
}