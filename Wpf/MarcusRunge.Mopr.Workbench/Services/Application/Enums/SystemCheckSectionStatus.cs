using MarcusRunge.Mopr.Workbench.Services.Application.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Enums;

/// <summary>
/// Defines the user-oriented state of one system-check section.
/// </summary>
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum SystemCheckSectionStatus
{
    /// <summary>
    /// The section has not been checked.
    /// </summary>
    [LocalizedDescription("SystemCheckSectionStatus_NotStarted", typeof(Resources))]
    NotStarted,

    /// <summary>
    /// The section is currently being checked.
    /// </summary>
    [LocalizedDescription("SystemCheckSectionStatus_Running", typeof(Resources))]
    Running,

    /// <summary>
    /// The section completed without findings.
    /// </summary>
    [LocalizedDescription("SystemCheckSectionStatus_Passed", typeof(Resources))]
    Passed,

    /// <summary>
    /// The section completed with non-critical information.
    /// </summary>
    [LocalizedDescription("SystemCheckSectionStatus_Notice", typeof(Resources))]
    Notice,

    /// <summary>
    /// The section completed with a warning that should be reviewed.
    /// </summary>
    [LocalizedDescription("SystemCheckSectionStatus_Warning", typeof(Resources))]
    Warning,

    /// <summary>
    /// The section contains a blocking finding that requires action.
    /// </summary>
    [LocalizedDescription("SystemCheckSectionStatus_ActionRequired", typeof(Resources))]
    ActionRequired,

    /// <summary>
    /// The section could not be checked because a required service or configuration was unavailable.
    /// </summary>
    [LocalizedDescription("SystemCheckSectionStatus_Unavailable", typeof(Resources))]
    Unavailable,

    /// <summary>
    /// The section check was canceled.
    /// </summary>
    [LocalizedDescription("SystemCheckSectionStatus_Canceled", typeof(Resources))]
    Canceled,

    /// <summary>
    /// The section check failed unexpectedly.
    /// </summary>
    [LocalizedDescription("SystemCheckSectionStatus_Failed", typeof(Resources))]
    Failed
}
