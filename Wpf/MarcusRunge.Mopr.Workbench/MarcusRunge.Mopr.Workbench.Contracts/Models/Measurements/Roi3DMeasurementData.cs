using MarcusRunge.Mopr.Workbench.Contracts.Models.Geometry;
using System.Collections.Generic;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements
{
    public class Roi3DMeasurementData : MeasurementData
    {
        public IList<Point3D> Vertices { get; set; } = new List<Point3D>();
    }
}