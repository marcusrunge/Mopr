using MarcusRunge.Mopr.Workbench.Services.Repository.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Enums
{
    /// <summary>
    /// Defines the operational alert level of a MIRAS issue.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum MirasAlertLevel
    {
        /// <summary>
        /// Indicates a normal operational state.
        /// </summary>
        [LocalizedDescription("MirasAlertLevel_Normal", typeof(Resources))]
        Normal,

        /// <summary>
        /// Provides information that does not require user action.
        /// </summary>
        [LocalizedDescription("MirasAlertLevel_Advisory", typeof(Resources))]
        Advisory,

        /// <summary>
        /// Indicates a condition that should be reviewed.
        /// </summary>
        [LocalizedDescription("MirasAlertLevel_Caution", typeof(Resources))]
        Caution,

        /// <summary>
        /// Indicates a condition that prevents safe or reliable use
        /// of the affected image data.
        /// </summary>
        [LocalizedDescription("MirasAlertLevel_Warning", typeof(Resources))]
        Warning
    }
}