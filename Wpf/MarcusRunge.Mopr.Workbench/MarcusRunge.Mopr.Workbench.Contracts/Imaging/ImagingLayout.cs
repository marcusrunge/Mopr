using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Imaging
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ImagingLayout
    {
        [LocalizedDescription("Viewer_Layout_Single", typeof(Resources))]
        Single = 0,
        [LocalizedDescription("Viewer_Layout_TwoByTwo", typeof(Resources))]
        TwoByTwo = 1,
        [LocalizedDescription("Viewer_Layout_Mpr", typeof(Resources))]
        Mpr = 2,
        [LocalizedDescription("Viewer_Layout_AxialSagittalCoronal", typeof(Resources))]
        AxialSagittalCoronal = 3,
        [LocalizedDescription("Viewer_Layout_Unknown", typeof(Resources))]
        Unknown = 4
    }
}