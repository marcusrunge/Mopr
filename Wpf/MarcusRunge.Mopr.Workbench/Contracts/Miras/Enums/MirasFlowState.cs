using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Miras.Enums
{
    /// <summary>
    /// Defines the execution state of the MIRAS flow.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum MirasFlowState
    {
        /// <summary>
        /// No MIRAS check has been started.
        /// </summary>
        [LocalizedDescription("MirasFlowState_Idle", typeof(Resources))]
        Idle = 0,

        /// <summary>
        /// A MIRAS check is currently running.
        /// </summary>
        [LocalizedDescription("MirasFlowState_Running", typeof(Resources))]
        Running = 1,

        /// <summary>
        /// The MIRAS service returned an operation result.
        /// </summary>
        [LocalizedDescription("MirasFlowState_Completed", typeof(Resources))]
        Completed = 2,

        /// <summary>
        /// The MIRAS check was canceled.
        /// </summary>
        [LocalizedDescription("MirasFlowState_Canceled", typeof(Resources))]
        Canceled = 3,

        /// <summary>
        /// An unexpected exception escaped from the MIRAS service.
        /// </summary>
        [LocalizedDescription("MirasFlowState_Failed", typeof(Resources))]
        Failed = 4
    }
}