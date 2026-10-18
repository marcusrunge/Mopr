using MarcusRunge.Mopr.Workbench.Services.Application.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Enums;

/// <summary>
/// Defines the structured outcome of administratively creating a MOPR user.
/// </summary>
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum UserAdministrationCreateStatus
{
    /// <summary>
    /// The operation has not started.
    /// </summary>
    [LocalizedDescription("UserAdministrationCreateStatus_NotStarted", typeof(Resources))]
    NotStarted,

    /// <summary>
    /// The user was created successfully.
    /// </summary>
    [LocalizedDescription("UserAdministrationCreateStatus_Completed", typeof(Resources))]
    Completed,

    /// <summary>
    /// The supplied user values are invalid.
    /// </summary>
    [LocalizedDescription("UserAdministrationCreateStatus_ValidationFailed", typeof(Resources))]
    ValidationFailed,

    /// <summary>
    /// The reserved technical MOPR system identity was supplied.
    /// </summary>
    [LocalizedDescription("UserAdministrationCreateStatus_SystemUserReserved", typeof(Resources))]
    SystemUserReserved,

    /// <summary>
    /// Another user already uses the supplied operating-system login name.
    /// </summary>
    [LocalizedDescription("UserAdministrationCreateStatus_DuplicateLoginName", typeof(Resources))]
    DuplicateLoginName,

    /// <summary>
    /// Another user already uses the supplied Windows security identifier.
    /// </summary>
    [LocalizedDescription("UserAdministrationCreateStatus_DuplicateSecurityIdentifier", typeof(Resources))]
    DuplicateSecurityIdentifier,

    /// <summary>
    /// Another user already uses the supplied personnel number.
    /// </summary>
    [LocalizedDescription("UserAdministrationCreateStatus_DuplicatePersonnelNumber", typeof(Resources))]
    DuplicatePersonnelNumber,

    /// <summary>
    /// No valid persistent audit identity is available.
    /// </summary>
    [LocalizedDescription("UserAdministrationCreateStatus_Unauthorized", typeof(Resources))]
    Unauthorized,

    /// <summary>
    /// Persistence rejected the user because a unique value changed concurrently.
    /// </summary>
    [LocalizedDescription("UserAdministrationCreateStatus_Conflict", typeof(Resources))]
    Conflict,

    /// <summary>
    /// The user could not be created.
    /// </summary>
    [LocalizedDescription("UserAdministrationCreateStatus_Failed", typeof(Resources))]
    Failed
}