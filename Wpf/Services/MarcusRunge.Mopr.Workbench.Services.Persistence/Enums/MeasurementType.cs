using MarcusRunge.Mopr.Workbench.Services.Persistence.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Enums
{
    /// <summary>
    /// Represents the type of a measurement.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum MeasurementType
    {
        /// <summary>
        /// Gets the unknown measurement type.
        /// </summary>
        [LocalizedDescription("MeasurementType_Unknown", typeof(Resources))]
        Unknown = 0,
        /// <summary>
        /// Gets the length measurement type.
        /// </summary>
        [LocalizedDescription("MeasurementType_Length", typeof(Resources))]
        Length = 1,
        /// <summary>
        /// Gets the angle measurement type.
        /// </summary>
        [LocalizedDescription("MeasurementType_Angle", typeof(Resources))]
        Angle = 2,
        /// <summary>
        /// Gets the area measurement type.
        /// </summary>
        [LocalizedDescription("MeasurementType_Area", typeof(Resources))]
        Area = 3,
        /// <summary>
        /// Gets the ellipse measurement type.
        /// </summary>
        [LocalizedDescription("MeasurementType_Ellipse", typeof(Resources))]
        Ellipse = 4,
        /// <summary>
        /// Gets the rectangle measurement type.
        /// </summary>
        [LocalizedDescription("MeasurementType_Rectangle", typeof(Resources))]
        Rectangle = 5,
        /// <summary>
        /// Gets the polygon measurement type.
        /// </summary>
        [LocalizedDescription("MeasurementType_Polygon", typeof(Resources))]
        Polygon = 6,

        /// <summary>
        /// Gets the annotation measurement type.
        /// </summary>
        [LocalizedDescription("MeasurementType_Annotation", typeof(Resources))]
        Annotation = 7,

        /// <summary>
        /// Gets the volume measurement type.
        /// </summary>
        [LocalizedDescription("MeasurementType_Volume", typeof(Resources))]
        Volume = 8,

        /// <summary>
        /// Gets the 3D ROI measurement type.
        /// </summary>
        [LocalizedDescription("MeasurementType_Roi3D", typeof(Resources))]
        Roi3D = 9,

        /// <summary>
        /// Gets the unreal object measurement type.
        /// </summary>
        [LocalizedDescription("MeasurementType_UnrealObject", typeof(Resources))]
        UnrealObject = 10
    }
}