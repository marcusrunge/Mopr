using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Enums
{
    /// <summary>
    /// Describes the assignment state between the current operating-system identity and a persistent MOPR user.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum UserAssignmentStatus
    {
        /// <summary>
        /// The current operating-system identity has not been resolved yet.
        /// </summary>
        [LocalizedDescription("UserAssignmentStatus_NotResolved", typeof(Resources))]
        NotResolved = 0,

        /// <summary>
        /// No usable operating-system identity is available.
        /// </summary>
        [LocalizedDescription("UserAssignmentStatus_OperatingSystemIdentityUnavailable", typeof(Resources))]
        OperatingSystemIdentityUnavailable = 1,

        /// <summary>
        /// No persistent MOPR user is assigned to the operating-system identity.
        /// </summary>
        [LocalizedDescription("UserAssignmentStatus_UserUnknown", typeof(Resources))]
        UserUnknown = 2,

        /// <summary>
        /// The assigned persistent MOPR user is inactive.
        /// </summary>
        [LocalizedDescription("UserAssignmentStatus_UserInactive", typeof(Resources))]
        UserInactive = 3,

        /// <summary>
        /// The assigned persistent MOPR user is active and available.
        /// </summary>
        [LocalizedDescription("UserAssignmentStatus_UserActive", typeof(Resources))]
        UserActive = 4,

        /// <summary>
        /// The assigned user does not have a valid persistent identifier.
        /// </summary>
        [LocalizedDescription("UserAssignmentStatus_InvalidPersistentUserId", typeof(Resources))]
        InvalidPersistentUserId = 5,

        /// <summary>
        /// Persistence is not available for user resolution.
        /// </summary>
        [LocalizedDescription("UserAssignmentStatus_PersistenceUnavailable", typeof(Resources))]
        PersistenceUnavailable = 6
    }
}
