using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Represents a DICOM import request.
    /// </summary>
    public sealed class DicomImportRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether existing instances may be overwritten.
        /// </summary>
        public bool AllowOverwrite { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user executing the import.
        /// </summary>
        public int CreatedByUserId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether repository repair should be executed after import.
        /// </summary>
        public bool ExecuteRepositoryRepair { get; set; }

        /// <summary>
        /// Gets or sets the ID of the repository location selected as the import target.
        /// </summary>
        public int RepositoryLocationId { get; set; }

        /// <summary>
        /// Gets or sets the source path.
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source type.
        /// </summary>
        public ImportSourceType SourceType { get; set; }
    }
}