namespace MarcusRunge.Mopr.Workbench.Services.Repository.Contracts
{
    /// <summary>
    /// Defines the public contract of the DICOM repository service.
    /// </summary>
    public interface IDicomRepositoryService
    {
        /// <summary>
        /// Creates the relative path.
        /// </summary>
        /// <param name="studyInstanceUid">The study instance uid.</param>
        /// <param name="seriesInstanceUid">The series instance uid.</param>
        /// <param name="sopInstanceUid">The sop instance uid.</param>
        /// <returns>The relative path.</returns>
        string CreateRelativePath(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid);

        /// <summary>
        /// Determines whether the specified relative path exists.
        /// </summary>
        /// <param name="relativePath">The relative path.</param>
        /// <returns><c>true</c> if the relative path exists; otherwise, <c>false</c>.</returns>
        bool Exists(string relativePath);

        /// <summary>
        /// Gets the absolute path.
        /// </summary>
        /// <param name="relativePath">The relative path.</param>
        /// <returns>The absolute path.</returns>
        string GetAbsolutePath(string relativePath);
    }
}