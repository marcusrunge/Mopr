namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Represents a resolved DICOM path within a specific repository location.
    /// </summary>
    public sealed class DicomRepositoryPathInfo
    {
        /// <summary>
        /// Gets or sets the absolute physical file path.
        /// </summary>
        public string AbsolutePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path relative to the repository-location root.
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the repository location used to resolve the path.
        /// </summary>
        public int RepositoryLocationId { get; set; }

        /// <summary>
        /// Gets or sets the normalized absolute root path of the repository location.
        /// </summary>
        public string RepositoryRootPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Series Instance UID.
        /// </summary>
        public string SeriesInstanceUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SOP Instance UID.
        /// </summary>
        public string SopInstanceUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Study Instance UID.
        /// </summary>
        public string StudyInstanceUid { get; set; } = string.Empty;
    }
}