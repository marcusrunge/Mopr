using MarcusRunge.Mopr.Workbench.Contracts.Models.Geometry;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements
{
    public class EllipseMeasurementData : MeasurementData
    {
        public Point2D Center { get; set; } = new Point2D();
        public double RadiusX { get; set; }
        public double RadiusY { get; set; }
        public double RotationDegrees { get; set; }
    }
}