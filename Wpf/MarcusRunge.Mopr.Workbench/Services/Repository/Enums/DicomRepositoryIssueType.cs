namespace MarcusRunge.Mopr.Workbench.Services.Repository.Enums
{
    /// <summary>
    /// Defines a technical repository integrity condition detected
    /// during repository verification or repair.
    /// </summary>
    public enum DicomRepositoryIssueType
    {
        /// <summary>
        /// No specific issue type has been assigned.
        /// </summary>
        Unknown,

        /// <summary>
        /// A persisted DICOM instance has no discoverable physical file.
        /// </summary>
        MissingFile,

        /// <summary>
        /// A DICOM file was found outside its expected repository location.
        /// </summary>
        MisplacedFile,

        /// <summary>
        /// Multiple physical files contain the same SOP instance UID.
        /// </summary>
        DuplicateFile,

        /// <summary>
        /// A file at an expected location contains an unexpected SOP instance UID.
        /// </summary>
        IdentityMismatch,

        /// <summary>
        /// A valid DICOM file has no corresponding persisted instance.
        /// </summary>
        OrphanedFile,

        /// <summary>
        /// A file expected to contain DICOM data is not a valid DICOM file.
        /// </summary>
        InvalidDicomFile,

        /// <summary>
        /// A repository file could not be read or inspected.
        /// </summary>
        UnreadableFile,

        /// <summary>
        /// A temporary file from an incomplete import operation was found.
        /// </summary>
        IncompleteImport,

        /// <summary>
        /// Persisted study, series, instance or dependent entity relationships are inconsistent.
        /// </summary>
        RelationshipConflict
    }
}