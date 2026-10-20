using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Enums;

/// <summary>
/// Defines the structured outcome of an administrative security-configuration update.
/// </summary>
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum SecurityConfigurationUpdateStatus
{
    /// <summary>
    /// The update has not started.
    /// </summary>
    [LocalizedDescription("SecurityConfigurationUpdateStatus_NotStarted", typeof(Resources))]
    NotStarted,

    /// <summary>
    /// The security configuration was updated successfully.
    /// </summary>
    [LocalizedDescription("SecurityConfigurationUpdateStatus_Completed", typeof(Resources))]
    Completed,

    /// <summary>
    /// Elevated administrative authorization is required.
    /// </summary>
    [LocalizedDescription("SecurityConfigurationUpdateStatus_Unauthorized", typeof(Resources))]
    Unauthorized,

    /// <summary>
    /// The current machine configuration could not be loaded.
    /// </summary>
    [LocalizedDescription("SecurityConfigurationUpdateStatus_ConfigurationUnavailable", typeof(Resources))]
    ConfigurationUnavailable,

    /// <summary>
    /// The resulting machine configuration is invalid.
    /// </summary>
    [LocalizedDescription("SecurityConfigurationUpdateStatus_ValidationFailed", typeof(Resources))]
    ValidationFailed,

    /// <summary>
    /// The security configuration could not be updated.
    /// </summary>
    [LocalizedDescription("SecurityConfigurationUpdateStatus_Failed", typeof(Resources))]
    Failed
}