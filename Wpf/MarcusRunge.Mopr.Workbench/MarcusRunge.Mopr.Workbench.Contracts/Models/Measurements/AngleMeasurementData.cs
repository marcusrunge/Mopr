using MarcusRunge.Mopr.Workbench.Contracts.Models.Geometry;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements
{
    public class AngleMeasurementData : MeasurementData
    {
        public Point2D Vertex { get; set; } = new Point2D();
        public Point2D Point1 { get; set; } = new Point2D();
        public Point2D Point2 { get; set; } = new Point2D();
    }
}