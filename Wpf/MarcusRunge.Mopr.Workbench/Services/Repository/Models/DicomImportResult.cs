namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Represents the result of a DICOM import operation.
    /// </summary>
    public sealed class DicomImportResult
    {
        /// <summary>
        /// Gets the number of discovered files.
        /// </summary>
        public int DiscoveredFiles => Files.Count;

        /// <summary>
        /// Gets the collection of import errors.
        /// </summary>
        public IList<string> Errors { get; } = [];

        /// <summary>
        /// Gets or sets the number of failed files.
        /// </summary>
        public int FailedFiles { get; set; }

        /// <summary>
        /// Gets the collection of discovered file information.
        /// </summary>
        public IList<DicomImportFileInfo> Files { get; } = [];

        /// <summary>
        /// Gets the number of DICOM files that contain all identifiers
        /// required for repository import.
        /// </summary>
        public int ImportableFiles => Files.Count(fileInfo => fileInfo.IsImportable);

        /// <summary>
        /// Gets or sets the number of successfully imported files.
        /// </summary>
        public int ImportedFiles { get; set; }

        /// <summary>
        /// Gets or sets the number of skipped files.
        /// </summary>
        public int SkippedFiles { get; set; }

        /// <summary>
        /// Gets the number of valid DICOM files.
        /// </summary>
        public int ValidDicomFiles => Files.Count(fileInfo => fileInfo.IsDicomFile);
    }
}