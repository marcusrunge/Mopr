using MarcusRunge.Mopr.Workbench.Services.Miras.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Enums
{
    /// <summary>
    /// Defines the processing state of a MIRAS issue.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum MirasIssueState
    {
        /// <summary>
        /// The issue has been detected.
        /// </summary>
        [LocalizedDescription("MirasIssueState_Detected", typeof(Resources))]
        Detected,

        /// <summary>
        /// A safe resolution is available.
        /// </summary>
        [LocalizedDescription("MirasIssueState_ActionAvailable", typeof(Resources))]
        ActionAvailable,

        /// <summary>
        /// The condition requires an explicit decision.
        /// </summary>
        [LocalizedDescription("MirasIssueState_ActionRequired", typeof(Resources))]
        ActionRequired,

        /// <summary>
        /// MIRAS resolved the condition automatically.
        /// </summary>
        [LocalizedDescription("MirasIssueState_AutomaticallyResolved", typeof(Resources))]
        AutomaticallyResolved,

        /// <summary>
        /// The condition was resolved through an explicit user action.
        /// </summary>
        [LocalizedDescription("MirasIssueState_ManuallyResolved", typeof(Resources))]
        ManuallyResolved,

        /// <summary>
        /// Resolution has been intentionally deferred.
        /// </summary>
        [LocalizedDescription("MirasIssueState_Deferred", typeof(Resources))]
        Deferred,

        /// <summary>
        /// A resolution was attempted but failed.
        /// </summary>
        [LocalizedDescription("MirasIssueState_ResolutionFailed", typeof(Resources))]
        ResolutionFailed
    }
}