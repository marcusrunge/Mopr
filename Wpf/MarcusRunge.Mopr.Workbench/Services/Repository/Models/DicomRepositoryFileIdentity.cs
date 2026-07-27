namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Represents the complete DICOM hierarchy identity read from one physical repository file.
    /// </summary>
    internal sealed class DicomRepositoryFileIdentity
    {
        /// <summary>
        /// Gets or sets the Series Instance UID read from the physical file.
        /// </summary>
        internal string SeriesInstanceUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SOP Instance UID read from the physical file.
        /// </summary>
        internal string SopInstanceUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Study Instance UID read from the physical file.
        /// </summary>
        internal string StudyInstanceUid { get; set; } = string.Empty;
    }
}