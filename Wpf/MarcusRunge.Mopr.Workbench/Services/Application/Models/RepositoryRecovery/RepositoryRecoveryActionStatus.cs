using MarcusRunge.Mopr.Workbench.Services.Application.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.RepositoryRecovery;

/// <summary>
/// Defines the result of an explicit repository-location recovery action.
/// </summary>
[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum RepositoryRecoveryActionStatus
{
    [LocalizedDescription("RepositoryRecoveryActionStatus_Completed", typeof(Resources))]
    Completed,
    [LocalizedDescription("RepositoryRecoveryActionStatus_NotFound", typeof(Resources))]
    NotFound,
    [LocalizedDescription("RepositoryRecoveryActionStatus_Conflict", typeof(Resources))]
    Conflict,
    [LocalizedDescription("RepositoryRecoveryActionStatus_DefaultRepository", typeof(Resources))]
    DefaultRepository,
    [LocalizedDescription("RepositoryRecoveryActionStatus_InUse", typeof(Resources))]
    InUse,
    [LocalizedDescription("RepositoryRecoveryActionStatus_DuplicatePath", typeof(Resources))]
    DuplicatePath,
    [LocalizedDescription("RepositoryRecoveryActionStatus_ValidationFailed", typeof(Resources))]
    ValidationFailed,
    [LocalizedDescription("RepositoryRecoveryActionStatus_Unauthorized", typeof(Resources))]
    Unauthorized,
    [LocalizedDescription("RepositoryRecoveryActionStatus_Failed", typeof(Resources))]
    Failed
}
