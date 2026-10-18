using MarcusRunge.Mopr.Workbench.Services.Application.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Enums;

/// <summary>
/// Defines the structured outcome of an administrative user update.
/// </summary>
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum UserAdministrationUpdateStatus
{
    /// <summary>
    /// The update has not started.
    /// </summary>
    [LocalizedDescription("UserAdministrationUpdateStatus_NotStarted", typeof(Resources))]
    NotStarted,

    /// <summary>
    /// The user was updated successfully.
    /// </summary>
    [LocalizedDescription("UserAdministrationUpdateStatus_Completed", typeof(Resources))]
    Completed,

    /// <summary>
    /// The user no longer exists.
    /// </summary>
    [LocalizedDescription("UserAdministrationUpdateStatus_NotFound", typeof(Resources))]
    NotFound,

    /// <summary>
    /// The user changed after the administration view loaded it.
    /// </summary>
    [LocalizedDescription("UserAdministrationUpdateStatus_Conflict", typeof(Resources))]
    Conflict,

    /// <summary>
    /// The reserved technical MOPR system identity cannot be modified.
    /// </summary>
    [LocalizedDescription("UserAdministrationUpdateStatus_SystemUserProtected", typeof(Resources))]
    SystemUserProtected,

    /// <summary>
    /// The current user attempted to deactivate the current session identity.
    /// </summary>
    [LocalizedDescription("UserAdministrationUpdateStatus_SelfDeactivationNotAllowed", typeof(Resources))]
    SelfDeactivationNotAllowed,

    /// <summary>
    /// The current user is not permitted to modify the current session identity.
    /// </summary>
    [LocalizedDescription("UserAdministrationUpdateStatus_SelfModificationNotAllowed", typeof(Resources))]
    SelfModificationNotAllowed,

    /// <summary>
    /// Another user already uses the supplied operating-system login name.
    /// </summary>
    [LocalizedDescription("UserAdministrationUpdateStatus_DuplicateLoginName", typeof(Resources))]
    DuplicateLoginName,

    /// <summary>
    /// Another user already uses the supplied Windows security identifier.
    /// </summary>
    [LocalizedDescription("UserAdministrationUpdateStatus_DuplicateSecurityIdentifier", typeof(Resources))]
    DuplicateSecurityIdentifier,

    /// <summary>
    /// Another user already uses the supplied personnel number.
    /// </summary>
    [LocalizedDescription("UserAdministrationUpdateStatus_DuplicatePersonnelNumber", typeof(Resources))]
    DuplicatePersonnelNumber,

    /// <summary>
    /// No valid persistent audit identity is available.
    /// </summary>
    [LocalizedDescription("UserAdministrationUpdateStatus_Unauthorized", typeof(Resources))]
    Unauthorized,

    /// <summary>
    /// The supplied user values are invalid.
    /// </summary>
    [LocalizedDescription("UserAdministrationUpdateStatus_ValidationFailed", typeof(Resources))]
    ValidationFailed,

    /// <summary>
    /// The update failed for an unspecified reason.
    /// </summary>
    [LocalizedDescription("UserAdministrationUpdateStatus_Failed", typeof(Resources))]
    Failed
}