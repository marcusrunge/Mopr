using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Miras.Enums
{
    /// <summary>
    /// Defines the execution status of a MIRAS operation.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum MirasOperationStatus
    {
        /// <summary>
        /// The operation completed successfully without detecting issues or technical errors.
        /// </summary>
        [LocalizedDescription("MirasOperationStatus_Completed", typeof(Resources))]
        Completed = 0,

        /// <summary>
        /// The operation completed successfully and detected one or more integrity issues.
        /// </summary>
        [LocalizedDescription("MirasOperationStatus_CompletedWithIssues", typeof(Resources))]
        CompletedWithIssues = 1,

        /// <summary>
        /// The operation stopped safely because a prerequisite integrity check detected a blocking condition.
        /// </summary>
        [LocalizedDescription("MirasOperationStatus_Blocked", typeof(Resources))]
        Blocked = 2,

        /// <summary>
        /// The operation completed only partially because one or more technical errors prevented a complete integrity assessment.
        /// </summary>
        [LocalizedDescription("MirasOperationStatus_Incomplete", typeof(Resources))]
        Incomplete = 3,

        /// <summary>
        /// The operation could not complete because an unexpected technical failure occurred.
        /// </summary>
        [LocalizedDescription("MirasOperationStatus_Failed", typeof(Resources))]
        Failed = 4
    }
}