using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Imaging
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ImagingTool
    {
        [LocalizedDescription("Viewer_Tool_None", typeof(Resources))]
        None = 0,
        [LocalizedDescription("Viewer_Tool_Zoom", typeof(Resources))]
        Zoom = 1,
        [LocalizedDescription("Viewer_Tool_Pan", typeof(Resources))]
        Pan = 2,
        [LocalizedDescription("Viewer_Tool_WindowLevel", typeof(Resources))]
        WindowLevel = 3,
        [LocalizedDescription("Viewer_Tool_Crosshair", typeof(Resources))]
        Crosshair = 4,
        [LocalizedDescription("Viewer_Tool_Measure", typeof(Resources))]
        Measure = 5
    }
}