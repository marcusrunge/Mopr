using MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports;
using Prism.Mvvm;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    internal sealed class ViewportTransformState : BindableBase
    {
        public const double DefaultZoomStep = 1.25;

        private double _panOffsetX;
        private double _panOffsetY;
        private double _zoomFactor = ViewportImageGeometryCalculator.DefaultZoomFactor;
        private ViewportRotation _rotation;
        private bool _isFlippedHorizontally, _isFlippedVertically;

        public bool CanZoomIn => ZoomFactor < ViewportImageGeometryCalculator.MaximumZoomFactor;

        public bool CanZoomOut => ZoomFactor > ViewportImageGeometryCalculator.MinimumZoomFactor;

        public bool IsFlippedHorizontally
        {
            get => _isFlippedHorizontally;
            private set => SetProperty(ref _isFlippedHorizontally, value);
        }

        public bool IsFlippedVertically
        {
            get => _isFlippedVertically;
            private set => SetProperty(ref _isFlippedVertically, value);
        }

        public ViewportRotation Rotation
        {
            get => _rotation;
            private set => SetProperty(ref _rotation, value);
        }

        public double PanOffsetX
        {
            get => _panOffsetX;
            private set => SetProperty(ref _panOffsetX, NormalizeFiniteValue(value));
        }

        public double PanOffsetY
        {
            get => _panOffsetY;
            private set => SetProperty(ref _panOffsetY, NormalizeFiniteValue(value));
        }

        public double ZoomFactor
        {
            get => _zoomFactor;
            private set
            {
                var effectiveZoomFactor = ViewportImageGeometryCalculator.ClampZoomFactor(value);

                if (SetProperty(ref _zoomFactor, effectiveZoomFactor))
                {
                    RaisePropertyChanged(nameof(CanZoomIn));
                    RaisePropertyChanged(nameof(CanZoomOut));
                }
            }
        }

        public void AddPan(double horizontalDelta, double verticalDelta)
        {
            if (!double.IsFinite(horizontalDelta) || !double.IsFinite(verticalDelta)) return;

            PanOffsetX += horizontalDelta;
            PanOffsetY += verticalDelta;
        }

        public void FlipHorizontal() => IsFlippedHorizontally = !IsFlippedHorizontally;

        public void FlipVertical() => IsFlippedVertically = !IsFlippedVertically;

        public void Reset()
        {
            ResetRotation();
            ResetFlip();
            ZoomFactor = ViewportImageGeometryCalculator.DefaultZoomFactor;
            ResetPan();
        }

        public void ResetFlip()
        {
            IsFlippedHorizontally = false;
            IsFlippedVertically = false;
        }

        public void ResetRotation() => Rotation = ViewportRotation.None;

        public void RotateLeft() => Rotation = (ViewportRotation)(((int)Rotation + 270) % 360);

        public void RotateRight() => Rotation = (ViewportRotation)(((int)Rotation + 90) % 360);

        public void ResetPan()
        {
            PanOffsetX = 0.0;
            PanOffsetY = 0.0;
        }

        public void SetPan(double panOffsetX, double panOffsetY)
        {
            PanOffsetX = panOffsetX;
            PanOffsetY = panOffsetY;
        }

        public void SetZoom(double zoomFactor) => ZoomFactor = zoomFactor;

        public void SetZoomAndPan(double zoomFactor, double panOffsetX, double panOffsetY)
        {
            ZoomFactor = zoomFactor;
            SetPan(panOffsetX, panOffsetY);
        }

        public void ZoomIn(double zoomStep = DefaultZoomStep)
        {
            if (!IsValidZoomStep(zoomStep)) return;
            ZoomFactor *= zoomStep;
        }

        public void ZoomOut(double zoomStep = DefaultZoomStep)
        {
            if (!IsValidZoomStep(zoomStep)) return;
            ZoomFactor /= zoomStep;
        }

        private static bool IsValidZoomStep(double zoomStep) => double.IsFinite(zoomStep) && zoomStep > 1.0;

        private static double NormalizeFiniteValue(double value) => double.IsFinite(value) ? value : 0.0;
    }
}