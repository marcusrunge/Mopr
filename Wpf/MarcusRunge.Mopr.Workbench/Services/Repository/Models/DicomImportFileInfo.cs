namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    public sealed class DicomImportFileInfo
    {
        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the file is a valid DICOM file.
        /// </summary>
        public bool IsDicomFile { get; set; }

        /// <summary>
        /// Gets a value indicating whether the DICOM file contains
        /// all identifiers required for repository import.
        /// </summary>
        public bool IsImportable => IsDicomFile && !string.IsNullOrWhiteSpace(StudyInstanceUid) && !string.IsNullOrWhiteSpace(SeriesInstanceUid) && !string.IsNullOrWhiteSpace(SopInstanceUid);

        /// <summary>
        /// Gets or sets the relative path of the imported file inside the repository.
        /// </summary>
        public string RelativeRepositoryPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the series instance uid.
        /// </summary>
        public string SeriesInstanceUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sop instance uid.
        /// </summary>
        public string SopInstanceUid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the study instance uid.
        /// </summary>
        public string StudyInstanceUid { get; set; } = string.Empty;
    }
}