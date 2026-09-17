using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Identity
{
    /// <summary>
    /// Describes the result of creating a persistent MOPR user for the current operating-system identity.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum UserProvisioningStatus
    {
        /// <summary>
        /// No provisioning operation has been performed.
        /// </summary>
        [LocalizedDescription("UserProvisioningStatus_NotStarted", typeof(Resources))]
        NotStarted = 0,

        /// <summary>
        /// The persistent MOPR user was created and signed in successfully.
        /// </summary>
        [LocalizedDescription("UserProvisioningStatus_Completed", typeof(Resources))]
        Completed = 1,

        /// <summary>
        /// One or more user-editable values are invalid.
        /// </summary>
        [LocalizedDescription("UserProvisioningStatus_ValidationFailed", typeof(Resources))]
        ValidationFailed = 2,

        /// <summary>
        /// No usable operating-system identity is available.
        /// </summary>
        [LocalizedDescription("UserProvisioningStatus_OperatingSystemIdentityUnavailable", typeof(Resources))]
        OperatingSystemIdentityUnavailable = 3,

        /// <summary>
        /// A persistent MOPR user already exists for the operating-system identity.
        /// </summary>
        [LocalizedDescription("UserProvisioningStatus_UserAlreadyExists", typeof(Resources))]
        UserAlreadyExists = 4,

        /// <summary>
        /// Persistence is not available for user provisioning.
        /// </summary>
        [LocalizedDescription("UserProvisioningStatus_PersistenceUnavailable", typeof(Resources))]
        PersistenceUnavailable = 5,

        /// <summary>
        /// The provisioning operation failed without exposing technical details.
        /// </summary>
        [LocalizedDescription("UserProvisioningStatus_Failed", typeof(Resources))]
        Failed = 6
    }
}