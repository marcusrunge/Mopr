using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Enums
{
    /// <summary>
    /// Represents the type of imaging tool.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ImagingTool
    {
        /// <summary>
        /// Represents no active imaging tool.
        /// </summary>
        [LocalizedDescription("Viewer_Tool_None", typeof(Resources))]
        None = 0,
        /// <summary>
        /// Represents the zoom tool.
        /// </summary>
        [LocalizedDescription("Viewer_Tool_Zoom", typeof(Resources))]
        Zoom = 1,
        /// <summary>
        /// Represents the pan tool.
        /// </summary>
        [LocalizedDescription("Viewer_Tool_Pan", typeof(Resources))]
        Pan = 2,
        /// <summary>
        /// Represents the window/level tool.
        /// </summary>
        [LocalizedDescription("Viewer_Tool_WindowLevel", typeof(Resources))]
        WindowLevel = 3,
        /// <summary>
        /// Represents the crosshair tool.
        /// </summary>
        [LocalizedDescription("Viewer_Tool_Crosshair", typeof(Resources))]
        Crosshair = 4,
        /// <summary>
        /// Represents the measure tool.
        /// </summary>
        [LocalizedDescription("Viewer_Tool_Measure", typeof(Resources))]
        Measure = 5
    }
}