using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using System;
using System.Windows;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports
{
    internal static class ViewportImageGeometryCalculator
    {
        public const double DefaultZoomFactor = 1.0;
        public const double MaximumZoomFactor = 16.0;
        public const double MinimumZoomFactor = 0.1;

        public static double ClampZoomFactor(double zoomFactor)
        {
            if (!double.IsFinite(zoomFactor)) return DefaultZoomFactor;
            return Math.Clamp(zoomFactor, MinimumZoomFactor, MaximumZoomFactor);
        }

        public static bool TryCreate(DicomImageFrame? frame, FrameworkElement? host, out ViewportImageGeometry geometry) => TryCreate(frame, host, DefaultZoomFactor, 0.0, 0.0, ViewportRotation.None, false, false, out geometry);

        public static bool TryCreate(DicomImageFrame? frame, FrameworkElement? host, double zoomFactor, double panOffsetX, double panOffsetY, out ViewportImageGeometry geometry) => TryCreate(frame, host, zoomFactor, panOffsetX, panOffsetY, ViewportRotation.None, false, false, out geometry);

        public static bool TryCreate(DicomImageFrame? frame, FrameworkElement? host, double zoomFactor, double panOffsetX, double panOffsetY, ViewportRotation rotation, bool isFlippedHorizontally, bool isFlippedVertically, out ViewportImageGeometry geometry)
        {
            geometry = default;

            if (frame == null || host == null) return false;
            if (frame.Width <= 0 || frame.Height <= 0 || host.ActualWidth <= 0 || host.ActualHeight <= 0) return false;
            if (!double.IsFinite(panOffsetX) || !double.IsFinite(panOffsetY)) return false;
            if (rotation is not (ViewportRotation.None or ViewportRotation.Clockwise90 or ViewportRotation.HalfTurn or ViewportRotation.CounterClockwise90)) return false;

            var displayFrameWidth = rotation is ViewportRotation.Clockwise90 or ViewportRotation.CounterClockwise90 ? frame.Height : frame.Width;
            var displayFrameHeight = rotation is ViewportRotation.Clockwise90 or ViewportRotation.CounterClockwise90 ? frame.Width : frame.Height;
            var fitScale = Math.Min(host.ActualWidth / displayFrameWidth, host.ActualHeight / displayFrameHeight);

            if (!double.IsFinite(fitScale) || fitScale <= 0) return false;

            var effectiveZoomFactor = ClampZoomFactor(zoomFactor);
            var effectiveScale = fitScale * effectiveZoomFactor;

            if (!double.IsFinite(effectiveScale) || effectiveScale <= 0) return false;

            var displayedWidth = displayFrameWidth * effectiveScale;
            var displayedHeight = displayFrameHeight * effectiveScale;
            var offsetX = ((host.ActualWidth - displayedWidth) / 2.0) + panOffsetX;
            var offsetY = ((host.ActualHeight - displayedHeight) / 2.0) + panOffsetY;

            geometry = new ViewportImageGeometry(fitScale, effectiveZoomFactor, offsetX, offsetY, frame.Width, frame.Height, rotation, isFlippedHorizontally, isFlippedVertically);
            return true;
        }

        public static Vector CalculateZoomAnchoredPanOffset(ViewportImageGeometry currentGeometry, double targetZoomFactor, Point anchorHostPoint, double hostWidth, double hostHeight)
        {
            var effectiveTargetZoomFactor = ClampZoomFactor(targetZoomFactor);
            var anchorImagePoint = currentGeometry.HostPointToImagePoint(anchorHostPoint);
            var anchorDisplayPoint = currentGeometry.ImagePointToDisplayPoint(anchorImagePoint.X, anchorImagePoint.Y);
            var targetScale = currentGeometry.FitScale * effectiveTargetZoomFactor;
            var targetCenteredOffsetX = (hostWidth - currentGeometry.DisplayFrameWidth * targetScale) / 2.0;
            var targetCenteredOffsetY = (hostHeight - currentGeometry.DisplayFrameHeight * targetScale) / 2.0;

            return new Vector(
                anchorHostPoint.X - targetCenteredOffsetX - anchorDisplayPoint.X * targetScale,
                anchorHostPoint.Y - targetCenteredOffsetY - anchorDisplayPoint.Y * targetScale);
        }
    }
}