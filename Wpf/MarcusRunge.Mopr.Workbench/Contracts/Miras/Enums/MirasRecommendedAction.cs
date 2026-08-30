using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Miras.Enums
{
    /// <summary>
    /// Defines the action recommended by MIRAS.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum MirasRecommendedAction
    {
        /// <summary>
        /// No further action is required.
        /// </summary>
        [LocalizedDescription("MirasRecommendedAction_None", typeof(Resources))]
        None,

        /// <summary>
        /// Search for a missing image file.
        /// </summary>
        [LocalizedDescription("MirasRecommendedAction_LocateFile", typeof(Resources))]
        LocateFile,

        /// <summary>
        /// Restore a file to its expected repository location.
        /// </summary>
        [LocalizedDescription("MirasRecommendedAction_RestoreExpectedLocation", typeof(Resources))]
        RestoreExpectedLocation,

        /// <summary>
        /// Rebuild a missing persistence entry.
        /// </summary>
        [LocalizedDescription("MirasRecommendedAction_RebuildPersistenceEntry", typeof(Resources))]
        RebuildPersistenceEntry,

        /// <summary>
        /// Retry the failed operation.
        /// </summary>
        [LocalizedDescription("MirasRecommendedAction_RetryOperation", typeof(Resources))]
        RetryOperation,

        /// <summary>
        /// Review an identity or relationship conflict.
        /// </summary>
        [LocalizedDescription("MirasRecommendedAction_ReviewConflict", typeof(Resources))]
        ReviewConflict,

        /// <summary>
        /// Review multiple physical copies of the same image.
        /// </summary>
        [LocalizedDescription("MirasRecommendedAction_ReviewDuplicate", typeof(Resources))]
        ReviewDuplicate,

        /// <summary>
        /// Review an invalid or unreadable image file.
        /// </summary>
        [LocalizedDescription("MirasRecommendedAction_ReviewInvalidFile", typeof(Resources))]
        ReviewInvalidFile,

        /// <summary>
        /// Check or restore the repository connection.
        /// </summary>
        [LocalizedDescription("MirasRecommendedAction_ReconnectRepository", typeof(Resources))]
        ReconnectRepository,

        /// <summary>
        /// Request assistance from an authorized administrator.
        /// </summary>
        [LocalizedDescription("MirasRecommendedAction_ContactAdministrator", typeof(Resources))]
        ContactAdministrator
    }
}