using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Enums
{
    /// <summary>
    /// Defines the outcome of a protected repository-infrastructure recovery operation.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum RepositoryInfrastructureRecoveryStatus
    {
        /// <summary>
        /// No recovery operation has been started.
        /// </summary>
        [LocalizedDescription("RepositoryInfrastructureRecoveryStatus_NotStarted", typeof(Resources))]
        NotStarted = 0,

        /// <summary>
        /// The repository infrastructure was restored successfully.
        /// </summary>
        [LocalizedDescription("RepositoryInfrastructureRecoveryStatus_Completed", typeof(Resources))]
        Completed = 1,

        /// <summary>
        /// Elevated administrative authorization is required.
        /// </summary>
        [LocalizedDescription("RepositoryInfrastructureRecoveryStatus_AdministrativeAuthorizationRequired", typeof(Resources))]
        AdministrativeAuthorizationRequired = 2,

        /// <summary>
        /// The selected repository path could not be validated.
        /// </summary>
        [LocalizedDescription("RepositoryInfrastructureRecoveryStatus_RepositoryValidationFailed", typeof(Resources))]
        RepositoryValidationFailed = 3,

        /// <summary>
        /// Repository infrastructure already exists and must not be overwritten.
        /// </summary>
        [LocalizedDescription("RepositoryInfrastructureRecoveryStatus_RepositoryAlreadyConfigured", typeof(Resources))]
        RepositoryAlreadyConfigured = 4,

        /// <summary>
        /// Required persistence services are unavailable.
        /// </summary>
        [LocalizedDescription("RepositoryInfrastructureRecoveryStatus_PersistenceUnavailable", typeof(Resources))]
        PersistenceUnavailable = 5,

        /// <summary>
        /// The recovery operation failed unexpectedly.
        /// </summary>
        [LocalizedDescription("RepositoryInfrastructureRecoveryStatus_Failed", typeof(Resources))]
        Failed = 6
    }
}