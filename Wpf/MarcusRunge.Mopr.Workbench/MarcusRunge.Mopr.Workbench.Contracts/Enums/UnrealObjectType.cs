using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Enums
{
    /// <summary>
    /// Represents the type of an Unreal object.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum UnrealObjectType
    {
        /// <summary>
        /// Gets the type for a static mesh.
        /// </summary>
        [LocalizedDescription("UnrealObjectType_StaticMesh", typeof(Resources))]
        StaticMesh = 0,

        /// <summary>
        /// Gets the type for a segmentation.
        /// </summary>
        [LocalizedDescription("UnrealObjectType_Segmentation", typeof(Resources))]
        Segmentation = 1,
        
        /// <summary>
        /// Gets the type for a point cloud.
        /// </summary>
        [LocalizedDescription("UnrealObjectType_PointCloud", typeof(Resources))]
        PointCloud = 2,

        /// <summary>
        /// Gets the type for a volume.
        /// </summary>
        [LocalizedDescription("UnrealObjectType_Volume", typeof(Resources))]
        Volume = 3
    }
}