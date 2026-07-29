using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Contracts
{
    /// <summary>
    /// Defines the public contract for resolving safe physical DICOM paths
    /// within persisted repository locations.
    /// </summary>
    public interface IDicomRepositoryService
    {
        /// <summary>
        /// Creates validated path information for a DICOM instance within the
        /// specified repository location.
        /// </summary>
        /// <param name="repositoryLocation">
        /// The persisted physical repository location.
        /// </param>
        /// <param name="studyInstanceUid">
        /// The Study Instance UID.
        /// </param>
        /// <param name="seriesInstanceUid">
        /// The Series Instance UID.
        /// </param>
        /// <param name="sopInstanceUid">
        /// The SOP Instance UID.
        /// </param>
        /// <returns>
        /// The validated repository path information.
        /// </returns>
        DicomRepositoryPathInfo CreatePathInfo(RepositoryLocation repositoryLocation, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid);

        /// <summary>
        /// Creates the canonical relative repository path for a DICOM instance.
        /// </summary>
        /// <param name="studyInstanceUid">
        /// The Study Instance UID.
        /// </param>
        /// <param name="seriesInstanceUid">
        /// The Series Instance UID.
        /// </param>
        /// <param name="sopInstanceUid">
        /// The SOP Instance UID.
        /// </param>
        /// <returns>
        /// The canonical relative repository path.
        /// </returns>
        string CreateRelativePath(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid);

        /// <summary>
        /// Determines whether the specified relative file path exists within
        /// the repository location.
        /// </summary>
        /// <param name="repositoryLocation">
        /// The persisted physical repository location.
        /// </param>
        /// <param name="relativePath">
        /// The path relative to the repository root.
        /// </param>
        /// <returns>
        /// <c>true</c> if the resolved physical file exists; otherwise,
        /// <c>false</c>.
        /// </returns>
        bool Exists(RepositoryLocation repositoryLocation, string relativePath);

        /// <summary>
        /// Resolves and validates an absolute physical path within the
        /// specified repository location.
        /// </summary>
        /// <param name="repositoryLocation">
        /// The persisted physical repository location.
        /// </param>
        /// <param name="relativePath">
        /// The path relative to the repository root.
        /// </param>
        /// <returns>
        /// The normalized absolute physical path.
        /// </returns>
        string GetAbsolutePath(RepositoryLocation repositoryLocation, string relativePath);
    }
}