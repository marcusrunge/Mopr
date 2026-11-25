using System;
using System.Windows;
using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports
{
    internal readonly struct ViewportImageGeometry(double fitScale, double zoomFactor, double offsetX, double offsetY, int frameWidth, int frameHeight, ViewportRotation rotation = ViewportRotation.None, bool isFlippedHorizontally = false, bool isFlippedVertically = false)
    {
        public double DisplayFrameHeight => IsQuarterTurn ? FrameWidth : FrameHeight;
        public double DisplayFrameWidth => IsQuarterTurn ? FrameHeight : FrameWidth;
        public double DisplayedHeight => DisplayFrameHeight * Scale;
        public double DisplayedWidth => DisplayFrameWidth * Scale;
        public double FitScale { get; } = fitScale;
        public int FrameHeight { get; } = frameHeight;
        public int FrameWidth { get; } = frameWidth;
        public bool IsFlippedHorizontally { get; } = isFlippedHorizontally;
        public bool IsFlippedVertically { get; } = isFlippedVertically;
        public double OffsetX { get; } = offsetX;
        public double OffsetY { get; } = offsetY;
        public ViewportImagePlacement Placement => new(DisplayedWidth, DisplayedHeight, FrameWidth * Scale, FrameHeight * Scale, OffsetX, OffsetY, ImageRenderTransform);
        public ViewportRotation Rotation { get; } = rotation;
        public double Scale => FitScale * ZoomFactor;
        public double ZoomFactor { get; } = zoomFactor;

        public Matrix ImageRenderTransform
        {
            get
            {
                var transform = ImageToDisplayMatrix;
                transform.OffsetX *= Scale;
                transform.OffsetY *= Scale;
                return transform;
            }
        }

        public bool ContainsHostPoint(Point hostPoint) => hostPoint.X >= OffsetX && hostPoint.Y >= OffsetY && hostPoint.X < OffsetX + DisplayedWidth && hostPoint.Y < OffsetY + DisplayedHeight;

        public bool ContainsImagePoint(double imageX, double imageY) => imageX >= 0 && imageY >= 0 && imageX < FrameWidth && imageY < FrameHeight;

        public Point HostPointToImagePoint(Point hostPoint)
        {
            var displayPoint = new Point((hostPoint.X - OffsetX) / Scale, (hostPoint.Y - OffsetY) / Scale);
            var inverseTransform = ImageToDisplayMatrix;
            inverseTransform.Invert();
            return inverseTransform.Transform(displayPoint);
        }

        public Point ImagePointToDisplayPoint(double imageX, double imageY) => ImageToDisplayMatrix.Transform(new Point(imageX, imageY));

        public Point ImagePointToHostPoint(double imageX, double imageY)
        {
            var displayPoint = ImagePointToDisplayPoint(imageX, imageY);
            return new Point(OffsetX + displayPoint.X * Scale, OffsetY + displayPoint.Y * Scale);
        }

        public int ToPixelX(double imageX) => Math.Clamp((int)Math.Floor(imageX), 0, FrameWidth - 1);

        public int ToPixelY(double imageY) => Math.Clamp((int)Math.Floor(imageY), 0, FrameHeight - 1);

        private bool IsQuarterTurn => Rotation is ViewportRotation.Clockwise90 or ViewportRotation.CounterClockwise90;

        private Matrix ImageToDisplayMatrix
        {
            get
            {
                var transform = Rotation switch
                {
                    ViewportRotation.None => Matrix.Identity,
                    ViewportRotation.Clockwise90 => new Matrix(0, 1, -1, 0, FrameHeight, 0),
                    ViewportRotation.HalfTurn => new Matrix(-1, 0, 0, -1, FrameWidth, FrameHeight),
                    ViewportRotation.CounterClockwise90 => new Matrix(0, -1, 1, 0, 0, FrameWidth),
                    _ => throw new InvalidOperationException("The viewport rotation is not supported.")
                };

                if (IsFlippedHorizontally)
                {
                    transform.M11 = -transform.M11;
                    transform.M21 = -transform.M21;
                    transform.OffsetX = DisplayFrameWidth - transform.OffsetX;
                }

                if (IsFlippedVertically)
                {
                    transform.M12 = -transform.M12;
                    transform.M22 = -transform.M22;
                    transform.OffsetY = DisplayFrameHeight - transform.OffsetY;
                }

                return transform;
            }
        }
    }
}
