using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Identity
{
    /// <summary>
    /// Identifies an invalid value in a user-provisioning request.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum UserProvisioningValidationIssue
    {
        /// <summary>
        /// The first name is missing.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_FirstNameRequired", typeof(Resources))]
        FirstNameRequired = 0,

        /// <summary>
        /// The first name exceeds the supported length.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_FirstNameTooLong", typeof(Resources))]
        FirstNameTooLong = 1,

        /// <summary>
        /// The last name is missing.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_LastNameRequired", typeof(Resources))]
        LastNameRequired = 2,

        /// <summary>
        /// The last name exceeds the supported length.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_LastNameTooLong", typeof(Resources))]
        LastNameTooLong = 3,

        /// <summary>
        /// The short name is missing.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_ShortNameRequired", typeof(Resources))]
        ShortNameRequired = 4,

        /// <summary>
        /// The short name exceeds the supported length.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_ShortNameTooLong", typeof(Resources))]
        ShortNameTooLong = 5,

        /// <summary>
        /// The short name contains unsupported control characters.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_ShortNameInvalid", typeof(Resources))]
        ShortNameInvalid = 6
    }
}