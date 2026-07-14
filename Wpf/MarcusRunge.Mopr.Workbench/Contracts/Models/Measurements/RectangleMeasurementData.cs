using MarcusRunge.Mopr.Workbench.Contracts.Models.Geometry;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements
{
    public class RectangleMeasurementData : MeasurementData
    {
        public Point2D Center { get; set; } = new Point2D();
        public double Height { get; set; }
        public double RotationDegrees { get; set; }
        public double Width { get; set; }
    }
}