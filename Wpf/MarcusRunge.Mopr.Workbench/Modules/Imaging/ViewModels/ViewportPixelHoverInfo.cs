namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ViewportPixelHoverInfo(string viewportId, int? pixelX, int? pixelY)
    {
        public bool HasPixel => PixelX.HasValue && PixelY.HasValue;
        public int? PixelX { get; } = pixelX;
        public int? PixelY { get; } = pixelY;
        public string ViewportId { get; } = viewportId;
    }
}