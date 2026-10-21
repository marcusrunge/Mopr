using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Enums;

/// <summary>
/// Defines the structured outcome of an administrative database-configuration update.
/// </summary>
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum DatabaseConfigurationUpdateStatus
{
    /// <summary>
    /// The update has not started.
    /// </summary>
    [LocalizedDescription("DatabaseConfigurationUpdateStatus_NotStarted", typeof(Resources))]
    NotStarted,

    /// <summary>
    /// The database configuration was updated successfully.
    /// </summary>
    [LocalizedDescription("DatabaseConfigurationUpdateStatus_Completed", typeof(Resources))]
    Completed,

    /// <summary>
    /// Elevated administrative authorization is required.
    /// </summary>
    [LocalizedDescription("DatabaseConfigurationUpdateStatus_Unauthorized", typeof(Resources))]
    Unauthorized,

    /// <summary>
    /// The current machine configuration could not be loaded.
    /// </summary>
    [LocalizedDescription("DatabaseConfigurationUpdateStatus_ConfigurationUnavailable", typeof(Resources))]
    ConfigurationUnavailable,

    /// <summary>
    /// The supplied database configuration is invalid.
    /// </summary>
    [LocalizedDescription("DatabaseConfigurationUpdateStatus_ValidationFailed", typeof(Resources))]
    ValidationFailed,

    /// <summary>
    /// The database configuration could not be updated.
    /// </summary>
    [LocalizedDescription("DatabaseConfigurationUpdateStatus_Failed", typeof(Resources))]
    Failed
}
