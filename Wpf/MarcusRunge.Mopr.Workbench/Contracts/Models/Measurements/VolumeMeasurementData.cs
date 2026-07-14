using MarcusRunge.Mopr.Workbench.Contracts.Models.Geometry;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements
{
    public class VolumeMeasurementData : MeasurementData
    {
        public Point3D Centroid { get; set; } = new Point3D();
        public double SurfaceArea { get; set; }
        public double Volume { get; set; }
    }
}