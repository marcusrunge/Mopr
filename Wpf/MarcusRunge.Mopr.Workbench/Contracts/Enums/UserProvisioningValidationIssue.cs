using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Enums
{
    /// <summary>
    /// Defines validation issues for user provisioning data.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum UserProvisioningValidationIssue
    {
        /// <summary>
        /// The first name is missing.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_FirstNameRequired", typeof(Resources))]
        FirstNameRequired,

        /// <summary>
        /// The first name exceeds the supported persistence length.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_FirstNameTooLong", typeof(Resources))]
        FirstNameTooLong,

        /// <summary>
        /// The last name is missing.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_LastNameRequired", typeof(Resources))]
        LastNameRequired,

        /// <summary>
        /// The last name exceeds the supported persistence length.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_LastNameTooLong", typeof(Resources))]
        LastNameTooLong,

        /// <summary>
        /// The short name is missing.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_ShortNameRequired", typeof(Resources))]
        ShortNameRequired,

        /// <summary>
        /// The short name exceeds the supported persistence length.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_ShortNameTooLong", typeof(Resources))]
        ShortNameTooLong,

        /// <summary>
        /// The short name contains unsupported characters.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_ShortNameInvalid", typeof(Resources))]
        ShortNameInvalid,

        /// <summary>
        /// The academic title exceeds the supported persistence length.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_AcademicTitleTooLong", typeof(Resources))]
        AcademicTitleTooLong,

        /// <summary>
        /// The personnel number exceeds the supported persistence length.
        /// </summary>
        [LocalizedDescription("UserProvisioningValidationIssue_PersonnelNumberTooLong", typeof(Resources))]
        PersonnelNumberTooLong
    }
}