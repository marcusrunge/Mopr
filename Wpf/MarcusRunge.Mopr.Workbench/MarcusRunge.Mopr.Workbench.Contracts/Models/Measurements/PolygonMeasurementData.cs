using MarcusRunge.Mopr.Workbench.Contracts.Models.Geometry;
using System.Collections.Generic;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements
{
    public class PolygonMeasurementData : MeasurementData
    {
        public IList<Point2D> Points { get; set; } = new List<Point2D>();
    }
}