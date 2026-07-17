using System;
using System.Collections.Generic;
using System.Text;

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