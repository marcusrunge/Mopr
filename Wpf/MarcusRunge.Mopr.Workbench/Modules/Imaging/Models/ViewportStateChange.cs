using MarcusRunge.Mopr.Workbench.Modules.Imaging.Enums;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Models
{
    /// <summary>Provides the common identity and origin of one atomic viewport state change.</summary>
    public abstract class ViewportStateChange
    {
        protected ViewportStateChange(string sourceViewportId, ViewportSynchronizationFlags synchronizationType, ViewportStateChangeOrigin origin)
        {
            if (string.IsNullOrWhiteSpace(sourceViewportId)) throw new ArgumentException("The source viewport ID must not be empty.", nameof(sourceViewportId));
            ViewportSynchronizationFlagsValidation.ValidateSingle(synchronizationType, nameof(synchronizationType));
            if (!Enum.IsDefined(origin)) throw new ArgumentOutOfRangeException(nameof(origin), origin, "The state change origin is not defined.");
            SourceViewportId = sourceViewportId;
            SynchronizationType = synchronizationType;
            Origin = origin;
        }

        public ViewportStateChangeOrigin Origin { get; }
        public string SourceViewportId { get; }
        public ViewportSynchronizationFlags SynchronizationType { get; }

        protected static double ValidateFinite(double value, string parameterName) => double.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(parameterName, value, "The value must be finite.");
    }

    public sealed class ViewportWindowLevelChange : ViewportStateChange
    {
        public ViewportWindowLevelChange(string sourceViewportId, double? windowCenter, double? windowWidth, ViewportStateChangeOrigin origin) : base(sourceViewportId, ViewportSynchronizationFlags.WindowLevel, origin)
        {
            if (windowCenter.HasValue != windowWidth.HasValue) throw new ArgumentException("Window center and width must either both be set or both be null.");
            if (windowCenter.HasValue) WindowCenter = ValidateFinite(windowCenter.Value, nameof(windowCenter));
            if (windowWidth.HasValue)
            {
                WindowWidth = ValidateFinite(windowWidth.Value, nameof(windowWidth));
                if (WindowWidth <= 0.0) throw new ArgumentOutOfRangeException(nameof(windowWidth), windowWidth, "Window width must be greater than zero.");
            }
        }

        public double? WindowCenter { get; }
        public double? WindowWidth { get; }
    }

    public sealed class ViewportZoomChange : ViewportStateChange
    {
        public ViewportZoomChange(string sourceViewportId, double zoomFactor, ViewportStateChangeOrigin origin) : base(sourceViewportId, ViewportSynchronizationFlags.Zoom, origin) => ZoomFactor = ValidateFinite(zoomFactor, nameof(zoomFactor));
        public double ZoomFactor { get; }
    }

    public sealed class ViewportPanChange : ViewportStateChange
    {
        public ViewportPanChange(string sourceViewportId, double panOffsetX, double panOffsetY, ViewportStateChangeOrigin origin) : base(sourceViewportId, ViewportSynchronizationFlags.Pan, origin) => (PanOffsetX, PanOffsetY) = (ValidateFinite(panOffsetX, nameof(panOffsetX)), ValidateFinite(panOffsetY, nameof(panOffsetY)));
        public double PanOffsetX { get; }
        public double PanOffsetY { get; }
    }

    public sealed class ViewportRotationChange : ViewportStateChange
    {
        public ViewportRotationChange(string sourceViewportId, ViewportRotation rotation, ViewportStateChangeOrigin origin) : base(sourceViewportId, ViewportSynchronizationFlags.Rotation, origin)
        {
            if (!Enum.IsDefined(rotation)) throw new ArgumentOutOfRangeException(nameof(rotation), rotation, "The viewport rotation is not defined.");
            Rotation = rotation;
        }
        public ViewportRotation Rotation { get; }
    }

    public sealed class ViewportHorizontalFlipChange : ViewportStateChange
    {
        public ViewportHorizontalFlipChange(string sourceViewportId, bool isFlippedHorizontally, ViewportStateChangeOrigin origin) : base(sourceViewportId, ViewportSynchronizationFlags.HorizontalFlip, origin) => IsFlippedHorizontally = isFlippedHorizontally;
        public bool IsFlippedHorizontally { get; }
    }

    public sealed class ViewportVerticalFlipChange : ViewportStateChange
    {
        public ViewportVerticalFlipChange(string sourceViewportId, bool isFlippedVertically, ViewportStateChangeOrigin origin) : base(sourceViewportId, ViewportSynchronizationFlags.VerticalFlip, origin) => IsFlippedVertically = isFlippedVertically;
        public bool IsFlippedVertically { get; }
    }

    public sealed class ViewportSliceChange : ViewportStateChange
    {
        public ViewportSliceChange(string sourceViewportId, int currentSlice, ViewportStateChangeOrigin origin) : base(sourceViewportId, ViewportSynchronizationFlags.Slice, origin)
        {
            if (currentSlice < 1) throw new ArgumentOutOfRangeException(nameof(currentSlice), currentSlice, "Current slice must be greater than zero.");
            CurrentSlice = currentSlice;
        }

        public int CurrentSlice { get; }
    }
}
