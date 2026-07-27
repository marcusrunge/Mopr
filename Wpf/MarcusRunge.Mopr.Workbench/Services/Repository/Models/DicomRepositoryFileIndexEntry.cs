namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Groups all physical repository locations that expose the same SOP Instance UID.
    /// </summary>
    internal sealed class DicomRepositoryFileIndexEntry
    {
        /// <summary>
        /// Gets the absolute physical paths of all discovered files with this SOP Instance UID.
        /// </summary>
        internal IList<string> FilePaths { get; } = [];

        /// <summary>
        /// Gets or sets the SOP Instance UID represented by this index entry.
        /// </summary>
        internal string SopInstanceUid { get; set; } = string.Empty;
    }
}