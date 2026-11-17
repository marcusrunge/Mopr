using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Enums
{
    /// <summary>
    /// Represents the persistence state of an imaging-session change.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ImagingSessionChangeState
    {
        /// <summary>
        /// Indicates that a new entity must be persisted.
        /// </summary>
        [LocalizedDescription("ImagingSessionChangeState_Created", typeof(Resources))]
        Created = 0,

        /// <summary>
        /// Indicates that an existing entity must be updated.
        /// </summary>
        [LocalizedDescription("ImagingSessionChangeState_Modified", typeof(Resources))]
        Modified = 1,

        /// <summary>
        /// Indicates that an existing entity must be deleted.
        /// </summary>
        [LocalizedDescription("ImagingSessionChangeState_Deleted", typeof(Resources))]
        Deleted = 2
    }
}
