using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Enums
{
    /// <summary>
    /// Defines the execution state of the application-level MIRAS flow.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum MirasApplicationFlowState
    {
        /// <summary>
        /// No MIRAS check has been started.
        /// </summary>
        [LocalizedDescription("MirasApplicationFlowState_Idle", typeof(Resources))]
        Idle = 0,

        /// <summary>
        /// A MIRAS check is currently running.
        /// </summary>
        [LocalizedDescription("MirasApplicationFlowState_Running", typeof(Resources))]
        Running = 1,

        /// <summary>
        /// The MIRAS service returned an operation result.
        /// </summary>
        [LocalizedDescription("MirasApplicationFlowState_Completed", typeof(Resources))]
        Completed = 2,

        /// <summary>
        /// The MIRAS check was canceled.
        /// </summary>
        [LocalizedDescription("MirasApplicationFlowState_Canceled", typeof(Resources))]
        Canceled = 3,

        /// <summary>
        /// An unexpected exception escaped from the MIRAS service.
        /// </summary>
        [LocalizedDescription("MirasApplicationFlowState_Failed", typeof(Resources))]
        Failed = 4
    }
}