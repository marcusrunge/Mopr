using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ViewportMeasurementState
    {
        public double? DistancePixels
        {
            get
            {
                if (!HasMeasurement)
                {
                    return null;
                }

                var dx = SecondPointX!.Value - FirstPointX!.Value;
                var dy = SecondPointY!.Value - FirstPointY!.Value;

                return Math.Sqrt(dx * dx + dy * dy);
            }
        }

        public int? FirstPointX { get; private set; }

        public int? FirstPointY { get; private set; }

        public bool HasFirstPoint => FirstPointX.HasValue && FirstPointY.HasValue;
        public bool HasMeasurement => HasFirstPoint && HasSecondPoint;
        public bool HasSecondPoint => SecondPointX.HasValue && SecondPointY.HasValue;
        public int? SecondPointX { get; private set; }

        public int? SecondPointY { get; private set; }

        public void Clear()
        {
            FirstPointX = null;
            FirstPointY = null;
            SecondPointX = null;
            SecondPointY = null;
        }

        public void SetNextPoint(int pixelX, int pixelY)
        {
            if (!HasFirstPoint || HasSecondPoint)
            {
                FirstPointX = pixelX;
                FirstPointY = pixelY;
                SecondPointX = null;
                SecondPointY = null;
                return;
            }

            SecondPointX = pixelX;
            SecondPointY = pixelY;
        }
    }
}