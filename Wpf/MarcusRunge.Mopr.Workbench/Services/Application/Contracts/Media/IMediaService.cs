namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Media
{
    /// <summary>
    /// Provides the public application use case for managing media files.
    /// </summary>
    public interface IMediaService
    {
        /// <summary>
        /// Gets the image source service.
        /// </summary>
        IImageSourceService? ImageSourceService { get; }
    }
}