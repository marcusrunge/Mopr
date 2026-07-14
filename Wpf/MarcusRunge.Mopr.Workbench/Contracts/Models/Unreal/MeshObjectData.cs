using MarcusRunge.Mopr.Workbench.Contracts.Models.Geometry;
using System.Collections.Generic;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models.Unreal
{
    public class MeshObjectData : UnrealObjectData
    {
        public IList<int> Triangles { get; set; } = new List<int>();
        public IList<Point3D> Vertices { get; set; } = new List<Point3D>();
    }
}