namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Enums
{
    /// <summary>
    /// Defines a technical integrity condition detected in Persistence.
    /// </summary>
    public enum PersistenceIntegrityIssueType
    {
        /// <summary>
        /// No specific issue type has been assigned.
        /// </summary>
        Unknown,

        /// <summary>
        /// A required persisted value is missing.
        /// </summary>
        MissingRequiredValue,

        /// <summary>
        /// A value that must be unique occurs more than once.
        /// </summary>
        DuplicateUniqueValue,

        /// <summary>
        /// A persisted entity references a parent entity that does not exist.
        /// </summary>
        MissingParent,

        /// <summary>
        /// An audit user reference does not identify an existing user.
        /// </summary>
        InvalidAuditReference
    }
}