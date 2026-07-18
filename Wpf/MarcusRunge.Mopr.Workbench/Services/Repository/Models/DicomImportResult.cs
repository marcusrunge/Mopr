namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Represents the result of a DICOM import operation.
    /// </summary>
    public sealed class DicomImportResult
    {
        /// <summary>
        /// Gets or sets the number of discovered files.
        /// </summary>
        public int DiscoveredFiles { get; set; }

        /// <summary>
        /// Gets the collection of import errors.
        /// </summary>
        public IList<string> Errors { get; } = [];

        /// <summary>
        /// Gets or sets the number of failed files.
        /// </summary>
        public int FailedFiles { get; set; }

        /// <summary>
        /// Gets or sets the number of successfully imported files.
        /// </summary>
        public int ImportedFiles { get; set; }

        /// <summary>
        /// Gets or sets the number of skipped files.
        /// </summary>
        public int SkippedFiles { get; set; }

        /// <summary>
        /// Gets or sets the number of valid DICOM files.
        /// </summary>
        public int ValidDicomFiles { get; set; }
    }
}