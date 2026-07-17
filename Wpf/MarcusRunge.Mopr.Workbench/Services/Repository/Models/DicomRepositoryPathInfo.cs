namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Represents a resolved DICOM repository path.
    /// </summary>
    public sealed class DicomRepositoryPathInfo
    {
        /// <summary>
        /// Gets or sets the absolute repository path.
        /// </summary>
        public string AbsolutePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the relative repository path.
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the series instance UID.
        /// </summary>
        public string SeriesInstanceUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SOP instance UID.
        /// </summary>
        public string SopInstanceUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the study instance UID.
        /// </summary>
        public string StudyInstanceUid { get; set; } = string.Empty;
    }
}