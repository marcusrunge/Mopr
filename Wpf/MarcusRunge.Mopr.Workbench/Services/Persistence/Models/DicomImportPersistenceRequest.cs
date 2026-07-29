namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Models
{
    /// <summary>
    /// Contains the validated medical and repository relationships required to
    /// persist one imported DICOM instance atomically.
    /// </summary>
    public sealed class DicomImportPersistenceRequest
    {
        /// <summary>
        /// Gets or sets the ID of the user executing the import.
        /// </summary>
        public int CreatedByUserId { get; set; }

        /// <summary>
        /// Gets or sets the canonical path relative to the selected repository
        /// location.
        /// </summary>
        public string RelativeFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the selected repository location.
        /// </summary>
        public int RepositoryLocationId { get; set; }

        /// <summary>
        /// Gets or sets the DICOM Series Instance UID.
        /// </summary>
        public string SeriesInstanceUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the DICOM SOP Instance UID.
        /// </summary>
        public string SopInstanceUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the DICOM Study Instance UID.
        /// </summary>
        public string StudyInstanceUid { get; set; } = string.Empty;
    }
}