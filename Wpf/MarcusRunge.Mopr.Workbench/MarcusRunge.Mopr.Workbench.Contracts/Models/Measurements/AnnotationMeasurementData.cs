using MarcusRunge.Mopr.Workbench.Contracts.Models.Geometry;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements
{
    public class AnnotationMeasurementData : MeasurementData
    {
        public Point2D Location { get; set; } = new Point2D();
        public string? Text { get; set; }
    }
}