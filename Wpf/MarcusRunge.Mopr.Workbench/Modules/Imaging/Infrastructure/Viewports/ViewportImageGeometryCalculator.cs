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

        public static bool TryCreate(DicomImageFrame? frame, FrameworkElement? host, out ViewportImageGeometry geometry) => TryCreate(frame, host, DefaultZoomFactor, panOffsetX: 0.0, panOffsetY: 0.0, out geometry);

        public static bool TryCreate(DicomImageFrame? frame, FrameworkElement? host, double zoomFactor, double panOffsetX, double panOffsetY, out ViewportImageGeometry geometry)
        {
            geometry = default;

            if (frame == null || host == null) return false;
            if (frame.Width <= 0 || frame.Height <= 0 || host.ActualWidth <= 0 || host.ActualHeight <= 0) return false;
            if (!double.IsFinite(panOffsetX) || !double.IsFinite(panOffsetY)) return false;

            var fitScale = Math.Min(host.ActualWidth / frame.Width, host.ActualHeight / frame.Height);

            if (!double.IsFinite(fitScale) || fitScale <= 0) return false;

            var effectiveZoomFactor = ClampZoomFactor(zoomFactor);
            var effectiveScale = fitScale * effectiveZoomFactor;

            if (!double.IsFinite(effectiveScale) || effectiveScale <= 0) return false;

            var displayedWidth = frame.Width * effectiveScale;
            var displayedHeight = frame.Height * effectiveScale;

            // Pan is represented in host coordinates so mouse movement and image movement remain one-to-one at every zoom level.
            var offsetX = ((host.ActualWidth - displayedWidth) / 2.0) + panOffsetX;
            var offsetY = ((host.ActualHeight - displayedHeight) / 2.0) + panOffsetY;

            geometry = new ViewportImageGeometry(fitScale, effectiveZoomFactor, offsetX, offsetY, frame.Width, frame.Height);
            return true;
        }

        public static Vector CalculateZoomAnchoredPanOffset(ViewportImageGeometry currentGeometry, double targetZoomFactor, Point anchorHostPoint, double hostWidth, double hostHeight)
        {
            var effectiveTargetZoomFactor = ClampZoomFactor(targetZoomFactor);
            var anchorImagePoint = currentGeometry.HostPointToImagePoint(anchorHostPoint);
            var targetScale = currentGeometry.FitScale * effectiveTargetZoomFactor;
            var targetCenteredOffsetX = (hostWidth - currentGeometry.FrameWidth * targetScale) / 2.0;
            var targetCenteredOffsetY = (hostHeight - currentGeometry.FrameHeight * targetScale) / 2.0;

            // Keeping the image point below the cursor stable prevents the viewport from appearing to jump while zooming.
            return new Vector(
                anchorHostPoint.X - targetCenteredOffsetX - anchorImagePoint.X * targetScale,
                anchorHostPoint.Y - targetCenteredOffsetY - anchorImagePoint.Y * targetScale);
        }
    }
}