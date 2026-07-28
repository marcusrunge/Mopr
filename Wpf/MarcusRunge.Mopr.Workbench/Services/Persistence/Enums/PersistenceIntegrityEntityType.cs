namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Enums
{
    /// <summary>
    /// Defines the persisted entity type affected by an integrity issue.
    /// </summary>
    public enum PersistenceIntegrityEntityType
    {
        /// <summary>
        /// No specific entity type has been assigned.
        /// </summary>
        Unknown,

        /// <summary>
        /// A persisted user is affected.
        /// </summary>
        User,

        /// <summary>
        /// A persisted DICOM study is affected.
        /// </summary>
        Study,

        /// <summary>
        /// A persisted DICOM series is affected.
        /// </summary>
        Series,

        /// <summary>
        /// A persisted DICOM instance is affected.
        /// </summary>
        Instance,

        /// <summary>
        /// A persisted measurement is affected.
        /// </summary>
        Measurement,

        /// <summary>
        /// A configured DICOM repository location is affected.
        /// </summary>
        RepositoryLocation,

        /// <summary>
        /// A persisted Unreal object descriptor is affected.
        /// </summary>
        UnrealObject
    }
}