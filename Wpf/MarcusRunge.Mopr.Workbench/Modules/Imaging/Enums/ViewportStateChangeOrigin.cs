using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Enums
{
    /// <summary>Identifies whether a viewport state change originated locally or from synchronization.</summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ViewportStateChangeOrigin
    {
        /// <summary>Identifies a change produced by a local viewport operation.</summary>
        [LocalizedDescription("ViewportStateChangeOrigin_Local", typeof(Resources))]
        Local = 0,

        /// <summary>Identifies a change applied to a synchronization target.</summary>
        [LocalizedDescription("ViewportStateChangeOrigin_Synchronization", typeof(Resources))]
        Synchronization = 1
    }
}
