using System;
using System.Windows;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports
{
    internal readonly struct ViewportImageGeometry(double fitScale, double zoomFactor, double offsetX, double offsetY, int frameWidth, int frameHeight)
    {
        public double DisplayedHeight => FrameHeight * Scale;
        public double DisplayedWidth => FrameWidth * Scale;
        public double FitScale { get; } = fitScale;
        public int FrameHeight { get; } = frameHeight;
        public int FrameWidth { get; } = frameWidth;
        public double OffsetX { get; } = offsetX;
        public double OffsetY { get; } = offsetY;
        public ViewportImagePlacement Placement => new(DisplayedWidth, DisplayedHeight, OffsetX, OffsetY);
        public double Scale => FitScale * ZoomFactor;
        public double ZoomFactor { get; } = zoomFactor;

        public bool ContainsHostPoint(Point hostPoint) => hostPoint.X >= OffsetX && hostPoint.Y >= OffsetY && hostPoint.X < OffsetX + DisplayedWidth && hostPoint.Y < OffsetY + DisplayedHeight;

        public bool ContainsImagePoint(double imageX, double imageY) => imageX >= 0 && imageY >= 0 && imageX < FrameWidth && imageY < FrameHeight;

        public Point HostPointToImagePoint(Point hostPoint) => new((hostPoint.X - OffsetX) / Scale, (hostPoint.Y - OffsetY) / Scale);

        public Point ImagePointToHostPoint(double imageX, double imageY) => new(OffsetX + imageX * Scale, OffsetY + imageY * Scale);

        public int ToPixelX(double imageX) => Math.Clamp((int)Math.Floor(imageX), 0, FrameWidth - 1);

        public int ToPixelY(double imageY) => Math.Clamp((int)Math.Floor(imageY), 0, FrameHeight - 1);
    }
}