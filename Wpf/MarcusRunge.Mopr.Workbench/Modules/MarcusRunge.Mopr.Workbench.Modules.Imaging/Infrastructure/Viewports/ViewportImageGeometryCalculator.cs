using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using System;
using System.Windows;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports
{
    internal static class ViewportImageGeometryCalculator
    {
        public static bool TryCreate(DicomImageFrame? frame, FrameworkElement? host, out ViewportImageGeometry geometry)
        {
            geometry = default;

            if (frame == null)
            {
                return false;
            }

            if (host == null)
            {
                return false;
            }

            if (frame.Width <= 0 || frame.Height <= 0 || host.ActualWidth <= 0 || host.ActualHeight <= 0)
            {
                return false;
            }

            var scale = Math.Min(host.ActualWidth / frame.Width, host.ActualHeight / frame.Height);

            if (scale <= 0)
            {
                return false;
            }

            var displayedWidth = frame.Width * scale;
            var displayedHeight = frame.Height * scale;

            var offsetX = (host.ActualWidth - displayedWidth) / 2.0;
            var offsetY = (host.ActualHeight - displayedHeight) / 2.0;

            geometry = new ViewportImageGeometry(scale, offsetX, offsetY, frame.Width, frame.Height);

            return true;
        }
    }
}