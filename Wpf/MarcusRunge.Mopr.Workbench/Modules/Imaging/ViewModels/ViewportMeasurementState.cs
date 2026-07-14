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

        public double? FirstPointX { get; private set; }

        public double? FirstPointY { get; private set; }

        public bool HasFirstPoint => FirstPointX.HasValue && FirstPointY.HasValue;
        public bool HasMeasurement => HasFirstPoint && HasSecondPoint;
        public bool HasSecondPoint => SecondPointX.HasValue && SecondPointY.HasValue;
        public double? SecondPointX { get; private set; }

        public double? SecondPointY { get; private set; }

        public void Clear()
        {
            FirstPointX = null;
            FirstPointY = null;
            SecondPointX = null;
            SecondPointY = null;
        }

        public void SetNextPoint(double imageX, double imageY)
        {
            if (!HasFirstPoint || HasSecondPoint)
            {
                FirstPointX = imageX;
                FirstPointY = imageY;
                SecondPointX = null;
                SecondPointY = null;
                return;
            }

            SecondPointX = imageX;
            SecondPointY = imageY;
        }
    }
}