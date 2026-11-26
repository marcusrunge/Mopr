using MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports;
using Prism.Mvvm;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    internal readonly record struct ViewportTransformSnapshot(double ZoomFactor, double PanOffsetX, double PanOffsetY, ViewportRotation Rotation, bool IsFlippedHorizontally, bool IsFlippedVertically);

    internal sealed class ViewportTransformState : BindableBase
    {
        public const double DefaultZoomStep = 1.25;
        private double _panOffsetX, _panOffsetY, _zoomFactor = ViewportImageGeometryCalculator.DefaultZoomFactor;
        private ViewportRotation _rotation;
        private bool _isFlippedHorizontally, _isFlippedVertically;

        public bool CanZoomIn => ZoomFactor < ViewportImageGeometryCalculator.MaximumZoomFactor;
        public bool CanZoomOut => ZoomFactor > ViewportImageGeometryCalculator.MinimumZoomFactor;
        public bool IsFlippedHorizontally => _isFlippedHorizontally;
        public bool IsFlippedVertically => _isFlippedVertically;
        public double PanOffsetX => _panOffsetX;
        public double PanOffsetY => _panOffsetY;
        public ViewportRotation Rotation => _rotation;
        public double ZoomFactor => _zoomFactor;
        public ViewportTransformSnapshot Snapshot => new(ZoomFactor, PanOffsetX, PanOffsetY, Rotation, IsFlippedHorizontally, IsFlippedVertically);

        public bool AddPan(double horizontalDelta, double verticalDelta) => double.IsFinite(horizontalDelta) && double.IsFinite(verticalDelta) && SetPan(PanOffsetX + horizontalDelta, PanOffsetY + verticalDelta);
        public bool FlipHorizontal() => SetHorizontalFlip(!IsFlippedHorizontally);
        public bool FlipVertical() => SetVerticalFlip(!IsFlippedVertically);
        public void Reset()
        {
            SetRotation(ViewportRotation.None);
            ResetFlip();
            SetZoom(ViewportImageGeometryCalculator.DefaultZoomFactor);
            ResetPan();
        }

        public void ResetFlip()
        {
            SetHorizontalFlip(false);
            SetVerticalFlip(false);
        }

        public bool ResetPan() => SetPan(0.0, 0.0);
        public bool ResetRotation() => SetRotation(ViewportRotation.None);
        public bool RotateLeft() => SetRotation((ViewportRotation)(((int)Rotation + 270) % 360));
        public bool RotateRight() => SetRotation((ViewportRotation)(((int)Rotation + 90) % 360));

        public bool SetHorizontalFlip(bool value)
        {
            if (_isFlippedHorizontally == value) return false;
            _isFlippedHorizontally = value;
            RaisePropertyChanged(nameof(IsFlippedHorizontally));
            return true;
        }

        public bool SetPan(double panOffsetX, double panOffsetY)
        {
            var effectiveX = NormalizeFiniteValue(panOffsetX);
            var effectiveY = NormalizeFiniteValue(panOffsetY);
            var xChanged = _panOffsetX != effectiveX;
            var yChanged = _panOffsetY != effectiveY;
            if (!xChanged && !yChanged) return false;
            // Both fields are committed first so every observer reads one coherent pan vector.
            _panOffsetX = effectiveX;
            _panOffsetY = effectiveY;
            if (xChanged) RaisePropertyChanged(nameof(PanOffsetX));
            if (yChanged) RaisePropertyChanged(nameof(PanOffsetY));
            return true;
        }

        public bool SetRotation(ViewportRotation value)
        {
            if (_rotation == value) return false;
            _rotation = value;
            RaisePropertyChanged(nameof(Rotation));
            return true;
        }

        public bool SetVerticalFlip(bool value)
        {
            if (_isFlippedVertically == value) return false;
            _isFlippedVertically = value;
            RaisePropertyChanged(nameof(IsFlippedVertically));
            return true;
        }

        public bool SetZoom(double zoomFactor)
        {
            var effectiveZoomFactor = ViewportImageGeometryCalculator.ClampZoomFactor(zoomFactor);
            if (_zoomFactor == effectiveZoomFactor) return false;
            _zoomFactor = effectiveZoomFactor;
            RaisePropertyChanged(nameof(ZoomFactor));
            RaisePropertyChanged(nameof(CanZoomIn));
            RaisePropertyChanged(nameof(CanZoomOut));
            return true;
        }

        public (bool ZoomChanged, bool PanChanged) SetZoomAndPan(double zoomFactor, double panOffsetX, double panOffsetY) => (SetZoom(zoomFactor), SetPan(panOffsetX, panOffsetY));
        public bool ZoomIn(double zoomStep = DefaultZoomStep) => IsValidZoomStep(zoomStep) && SetZoom(ZoomFactor * zoomStep);
        public bool ZoomOut(double zoomStep = DefaultZoomStep) => IsValidZoomStep(zoomStep) && SetZoom(ZoomFactor / zoomStep);
        private static bool IsValidZoomStep(double zoomStep) => double.IsFinite(zoomStep) && zoomStep > 1.0;
        private static double NormalizeFiniteValue(double value) => double.IsFinite(value) ? value : 0.0;
    }
}
