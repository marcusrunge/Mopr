namespace MarcusRunge.Mopr.Workbench.Services.Repository.Enums
{
    /// <summary>
    /// Represents the physical repository state established for one DICOM
    /// import before its Persistence operation is committed.
    /// </summary>
    internal enum DicomImportFileSystemState
    {
        /// <summary>
        /// No repository destination file was changed.
        /// </summary>
        None,

        /// <summary>
        /// The destination file was created by the current import.
        /// </summary>
        Created,

        /// <summary>
        /// An identical destination file already existed and was not changed.
        /// </summary>
        ExistingIdentical,

        /// <summary>
        /// A different destination file was replaced after its original content
        /// had been moved to a unique backup file.
        /// </summary>
        OverwrittenWithBackup
    }
}