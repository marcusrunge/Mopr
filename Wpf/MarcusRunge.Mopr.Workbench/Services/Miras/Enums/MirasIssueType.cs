using MarcusRunge.Mopr.Workbench.Services.Miras.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Services.Miras.Enums
{
    /// <summary>
    /// Defines the type of an issue detected by MIRAS.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum MirasIssueType
    {
        /// <summary>
        /// No issue type has been assigned.
        /// </summary>
        [LocalizedDescription("MirasIssueType_Unknown", typeof(Resources))]
        Unknown,

        /// <summary>
        /// A persisted image file could not be found.
        /// </summary>
        [LocalizedDescription("MirasIssueType_MissingFile", typeof(Resources))]
        MissingFile,

        /// <summary>
        /// An image file was found at an unexpected repository location.
        /// </summary>
        [LocalizedDescription("MirasIssueType_MisplacedFile", typeof(Resources))]
        MisplacedFile,

        /// <summary>
        /// Multiple physical files contain the same SOP instance UID.
        /// </summary>
        [LocalizedDescription("MirasIssueType_DuplicateFile", typeof(Resources))]
        DuplicateFile,

        /// <summary>
        /// A file at the expected location contains an unexpected DICOM identity.
        /// </summary>
        [LocalizedDescription("MirasIssueType_IdentityMismatch", typeof(Resources))]
        IdentityMismatch,

        /// <summary>
        /// A valid DICOM file has no corresponding persisted instance.
        /// </summary>
        [LocalizedDescription("MirasIssueType_OrphanedFile", typeof(Resources))]
        OrphanedFile,

        /// <summary>
        /// A file expected to contain DICOM data is not a valid DICOM file.
        /// </summary>
        [LocalizedDescription("MirasIssueType_InvalidDicomFile", typeof(Resources))]
        InvalidDicomFile,

        /// <summary>
        /// A repository file could not be read or inspected.
        /// </summary>
        [LocalizedDescription("MirasIssueType_UnreadableFile", typeof(Resources))]
        UnreadableFile,

        /// <summary>
        /// A temporary file was left behind by an incomplete import operation.
        /// </summary>
        [LocalizedDescription("MirasIssueType_IncompleteImport", typeof(Resources))]
        IncompleteImport,

        /// <summary>
        /// Persisted study, series or instance relationships are inconsistent.
        /// </summary>
        [LocalizedDescription("MirasIssueType_RelationshipConflict", typeof(Resources))]
        RelationshipConflict,

        /// <summary>
        /// The configured repository is unavailable.
        /// </summary>
        [LocalizedDescription("MirasIssueType_RepositoryUnavailable", typeof(Resources))]
        RepositoryUnavailable,

        /// <summary>
        /// The persistence service is unavailable.
        /// </summary>
        [LocalizedDescription("MirasIssueType_PersistenceUnavailable", typeof(Resources))]
        PersistenceUnavailable
    }
}