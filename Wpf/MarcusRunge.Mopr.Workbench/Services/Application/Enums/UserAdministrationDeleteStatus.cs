using MarcusRunge.Mopr.Workbench.Services.Application.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Enums;

/// <summary>
/// Defines the structured outcome of administratively removing a persisted MOPR user.
/// </summary>
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum UserAdministrationDeleteStatus
{
    /// <summary>
    /// The operation has not started.
    /// </summary>
    [LocalizedDescription("UserAdministrationDeleteStatus_NotStarted", typeof(Resources))]
    NotStarted,

    /// <summary>
    /// The user was removed successfully.
    /// </summary>
    [LocalizedDescription("UserAdministrationDeleteStatus_Completed", typeof(Resources))]
    Completed,

    /// <summary>
    /// The user no longer exists.
    /// </summary>
    [LocalizedDescription("UserAdministrationDeleteStatus_NotFound", typeof(Resources))]
    NotFound,

    /// <summary>
    /// The user changed after the administration view loaded it.
    /// </summary>
    [LocalizedDescription("UserAdministrationDeleteStatus_Conflict", typeof(Resources))]
    Conflict,

    /// <summary>
    /// The reserved technical MOPR system identity cannot be removed.
    /// </summary>
    [LocalizedDescription("UserAdministrationDeleteStatus_SystemUserProtected", typeof(Resources))]
    SystemUserProtected,

    /// <summary>
    /// The current application-session identity cannot be removed.
    /// </summary>
    [LocalizedDescription("UserAdministrationDeleteStatus_CurrentUserProtected", typeof(Resources))]
    CurrentUserProtected,

    /// <summary>
    /// Existing persistent data still refers to the user.
    /// </summary>
    [LocalizedDescription("UserAdministrationDeleteStatus_InUse", typeof(Resources))]
    InUse,

    /// <summary>
    /// No valid persistent audit identity is available.
    /// </summary>
    [LocalizedDescription("UserAdministrationDeleteStatus_Unauthorized", typeof(Resources))]
    Unauthorized,

    /// <summary>
    /// The supplied deletion request is invalid.
    /// </summary>
    [LocalizedDescription("UserAdministrationDeleteStatus_ValidationFailed", typeof(Resources))]
    ValidationFailed,

    /// <summary>
    /// The user could not be removed.
    /// </summary>
    [LocalizedDescription("UserAdministrationDeleteStatus_Failed", typeof(Resources))]
    Failed
}