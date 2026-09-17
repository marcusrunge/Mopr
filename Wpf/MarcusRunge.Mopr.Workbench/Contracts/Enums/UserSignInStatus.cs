using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Identity
{
    /// <summary>
    /// Describes the result of resolving the current operating-system identity to a persistent MOPR user.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum UserSignInStatus
    {
        /// <summary>
        /// No sign-in operation has been performed.
        /// </summary>
        [LocalizedDescription("UserSignInStatus_NotStarted", typeof(Resources))]
        NotStarted = 0,

        /// <summary>
        /// An active persistent MOPR user was resolved successfully.
        /// </summary>
        [LocalizedDescription("UserSignInStatus_SignedIn", typeof(Resources))]
        SignedIn = 1,

        /// <summary>
        /// No usable operating-system identity is available.
        /// </summary>
        [LocalizedDescription("UserSignInStatus_OperatingSystemIdentityUnavailable", typeof(Resources))]
        OperatingSystemIdentityUnavailable = 2,

        /// <summary>
        /// No persistent MOPR user is assigned to the operating-system identity.
        /// </summary>
        [LocalizedDescription("UserSignInStatus_UserUnknown", typeof(Resources))]
        UserUnknown = 3,

        /// <summary>
        /// The assigned persistent MOPR user is disabled.
        /// </summary>
        [LocalizedDescription("UserSignInStatus_UserDisabled", typeof(Resources))]
        UserDisabled = 4,

        /// <summary>
        /// The assigned MOPR user does not have a valid persistent identifier.
        /// </summary>
        [LocalizedDescription("UserSignInStatus_InvalidPersistentUserId", typeof(Resources))]
        InvalidPersistentUserId = 5,

        /// <summary>
        /// Persistence is not available for user resolution.
        /// </summary>
        [LocalizedDescription("UserSignInStatus_PersistenceUnavailable", typeof(Resources))]
        PersistenceUnavailable = 6,

        /// <summary>
        /// The sign-in operation failed without exposing technical details to the caller.
        /// </summary>
        [LocalizedDescription("UserSignInStatus_Failed", typeof(Resources))]
        Failed = 7
    }
}