using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration
{
    /// <summary>
    /// Defines the result status of a machine-wide setup-completion operation.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum SetupCompletionStatus
    {
        /// <summary>
        /// No setup-completion operation has been started.
        /// </summary>
        [LocalizedDescription("SetupCompletionStatus_NotStarted", typeof(Resources))]
        NotStarted = 0,

        /// <summary>
        /// The setup-completion operation completed successfully.
        /// </summary>
        [LocalizedDescription("SetupCompletionStatus_Completed", typeof(Resources))]
        Completed = 1,

        /// <summary>
        /// The database connection could not be validated.
        /// </summary>
        [LocalizedDescription("SetupCompletionStatus_DatabaseValidationFailed", typeof(Resources))]
        DatabaseValidationFailed = 2,

        /// <summary>
        /// The selected repository location could not be validated.
        /// </summary>
        [LocalizedDescription("SetupCompletionStatus_RepositoryValidationFailed", typeof(Resources))]
        RepositoryValidationFailed = 3,

        /// <summary>
        /// The setup-completion operation was canceled safely.
        /// </summary>
        [LocalizedDescription("SetupCompletionStatus_Canceled", typeof(Resources))]
        Canceled = 4,

        /// <summary>
        /// The setup-completion operation failed without leaving a known uncompensated change.
        /// </summary>
        [LocalizedDescription("SetupCompletionStatus_Failed", typeof(Resources))]
        Failed = 5,

        /// <summary>
        /// The setup-completion operation and the subsequent compensation both failed.
        /// </summary>
        [LocalizedDescription("SetupCompletionStatus_FailedAndRollbackFailed", typeof(Resources))]
        FailedAndRollbackFailed = 6
    }
}