using System.Windows;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports
{
    internal readonly struct ViewportImageGeometry(double scale, double offsetX, double offsetY, int frameWidth, int frameHeight)
    {
        public int FrameHeight { get; } = frameHeight;
        public int FrameWidth { get; } = frameWidth;
        public double OffsetX { get; } = offsetX;
        public double OffsetY { get; } = offsetY;
        public double Scale { get; } = scale;

        public int ToPixelX(double imageX) => (int)imageX;

        public int ToPixelY(double imageY) => (int)imageY;

        public bool ContainsHostPoint(Point hostPoint)
        {
            var displayedWidth = FrameWidth * Scale;
            var displayedHeight = FrameHeight * Scale;

            return hostPoint.X >= OffsetX && hostPoint.Y >= OffsetY && hostPoint.X < OffsetX + displayedWidth && hostPoint.Y < OffsetY + displayedHeight;
        }

        public bool ContainsImagePoint(double imageX, double imageY) => imageX >= 0 && imageY >= 0 && imageX < FrameWidth && imageY < FrameHeight;

        public Point HostPointToImagePoint(Point hostPoint) => new((hostPoint.X - OffsetX) / Scale, (hostPoint.Y - OffsetY) / Scale);

        public Point ImagePointToHostPoint(double imageX, double imageY) => new(OffsetX + imageX * Scale, OffsetY + imageY * Scale);
    }
}