using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Miras.Enums
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
        Unknown = 0,

        /// <summary>
        /// A persisted image file could not be found.
        /// </summary>
        [LocalizedDescription("MirasIssueType_MissingFile", typeof(Resources))]
        MissingFile = 1,

        /// <summary>
        /// An image file was found at an unexpected repository location.
        /// </summary>
        [LocalizedDescription("MirasIssueType_MisplacedFile", typeof(Resources))]
        MisplacedFile = 2,

        /// <summary>
        /// Multiple physical files contain the same SOP instance UID.
        /// </summary>
        [LocalizedDescription("MirasIssueType_DuplicateFile", typeof(Resources))]
        DuplicateFile = 3,

        /// <summary>
        /// A file at the expected location contains an unexpected DICOM identity.
        /// </summary>
        [LocalizedDescription("MirasIssueType_IdentityMismatch", typeof(Resources))]
        IdentityMismatch = 4,

        /// <summary>
        /// A valid DICOM file has no corresponding persisted instance.
        /// </summary>
        [LocalizedDescription("MirasIssueType_OrphanedFile", typeof(Resources))]
        OrphanedFile = 5,

        /// <summary>
        /// A file expected to contain DICOM data is not a valid DICOM file.
        /// </summary>
        [LocalizedDescription("MirasIssueType_InvalidDicomFile", typeof(Resources))]
        InvalidDicomFile = 6,

        /// <summary>
        /// A repository file could not be read or inspected.
        /// </summary>
        [LocalizedDescription("MirasIssueType_UnreadableFile", typeof(Resources))]
        UnreadableFile = 7,

        /// <summary>
        /// A temporary file was left behind by an incomplete import operation.
        /// </summary>
        [LocalizedDescription("MirasIssueType_IncompleteImport", typeof(Resources))]
        IncompleteImport = 8,

        /// <summary>
        /// Persisted study, series or instance relationships are inconsistent.
        /// </summary>
        [LocalizedDescription("MirasIssueType_RelationshipConflict", typeof(Resources))]
        RelationshipConflict = 9,

        /// <summary>
        /// The configured repository is unavailable.
        /// </summary>
        [LocalizedDescription("MirasIssueType_RepositoryUnavailable", typeof(Resources))]
        RepositoryUnavailable = 10,

        /// <summary>
        /// The persistence service is unavailable or could not complete its integrity assessment.
        /// </summary>
        [LocalizedDescription("MirasIssueType_PersistenceUnavailable", typeof(Resources))]
        PersistenceUnavailable = 11,

        /// <summary>
        /// A required persisted value is missing.
        /// </summary>
        [LocalizedDescription("MirasIssueType_PersistenceRequiredValueMissing", typeof(Resources))]
        PersistenceRequiredValueMissing = 12,

        /// <summary>
        /// A persisted value violates its validity requirements.
        /// </summary>
        [LocalizedDescription("MirasIssueType_PersistenceValueInvalid", typeof(Resources))]
        PersistenceValueInvalid = 13,

        /// <summary>
        /// A persisted value that must be unique occurs more than once.
        /// </summary>
        [LocalizedDescription("MirasIssueType_PersistenceUniqueValueConflict", typeof(Resources))]
        PersistenceUniqueValueConflict = 14,

        /// <summary>
        /// A required persisted parent-child relationship is missing or inconsistent.
        /// </summary>
        [LocalizedDescription("MirasIssueType_PersistenceRelationshipConflict", typeof(Resources))]
        PersistenceRelationshipConflict = 15,

        /// <summary>
        /// A persisted audit user reference is invalid.
        /// </summary>
        [LocalizedDescription("MirasIssueType_PersistenceAuditReferenceInvalid", typeof(Resources))]
        PersistenceAuditReferenceInvalid = 16
    }
}