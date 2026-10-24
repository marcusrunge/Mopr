using MarcusRunge.Mopr.Workbench.Services.Repository.Properties;
using MarcusRunge.Toolbox.Localization.Core;

using System.ComponentModel;
namespace MarcusRunge.Mopr.Workbench.Services.Repository.Enums
{
    /// <summary>
    /// Defines why a repository location could not be validated.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum RepositoryLocationValidationFailureKind
    {
        /// <summary>
        /// No validation failure occurred.
        /// </summary>
        [LocalizedDescription("RepositoryLocationValidationFailureKind_None", typeof(Resources))]
        None,

        /// <summary>
        /// The supplied path is empty or cannot be normalized.
        /// </summary>
        [LocalizedDescription("RepositoryLocationValidationFailureKind_InvalidPath", typeof(Resources))]
        InvalidPath,

        /// <summary>
        /// The local directory does not exist.
        /// </summary>
        [LocalizedDescription("RepositoryLocationValidationFailureKind_NotFound", typeof(Resources))]
        NotFound,

        /// <summary>
        /// The repository infrastructure is currently unavailable.
        /// </summary>
        [LocalizedDescription("RepositoryLocationValidationFailureKind_Unavailable", typeof(Resources))]
        Unavailable,

        /// <summary>
        /// The current process is not authorized to access the location.
        /// </summary>
        [LocalizedDescription("RepositoryLocationValidationFailureKind_AccessDenied", typeof(Resources))]
        AccessDenied,

        /// <summary>
        /// The location can be read but does not permit the required write operation.
        /// </summary>
        [LocalizedDescription("RepositoryLocationValidationFailureKind_ReadOnly", typeof(Resources))]
        ReadOnly
    }
}