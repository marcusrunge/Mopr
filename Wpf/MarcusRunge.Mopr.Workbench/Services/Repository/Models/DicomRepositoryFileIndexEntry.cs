namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Represents all physical repository files discovered for one
    /// SOP instance UID.
    /// </summary>
    internal sealed class DicomRepositoryFileIndexEntry
    {
        /// <summary>
        /// Gets the physical file paths containing the SOP instance UID.
        /// </summary>
        internal IList<string> FilePaths { get; } = [];

        /// <summary>
        /// Gets or sets the SOP instance UID represented by this entry.
        /// </summary>
        internal string SopInstanceUid { get; set; } = string.Empty;
    }
}