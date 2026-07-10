using MarcusRunge.Mopr.Workbench.Contracts.Models.Geometry;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements
{
    public class LengthMeasurementData : MeasurementData
    {
        public Point2D StartPoint { get; set; } = new Point2D();
        public Point2D EndPoint { get; set; } = new Point2D();
    }
}