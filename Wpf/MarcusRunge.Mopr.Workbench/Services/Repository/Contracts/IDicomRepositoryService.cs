using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Contracts
{
    /// <summary>
    /// Defines the public contract of the DICOM repository service.
    /// </summary>
    public interface IDicomRepositoryService
    {
        /// <summary>
        /// Creates the path information.
        /// </summary>
        /// <param name="studyInstanceUid">The study instance uid.</param>
        /// <param name="seriesInstanceUid">The series instance uid.</param>
        /// <param name="sopInstanceUid">The sop instance uid.</param>
        /// <returns>The DICOM repository path info.</returns>
        DicomRepositoryPathInfo CreatePathInfo(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid);

        /// <summary>
        /// Creates path information for a DICOM instance within the specified
        /// repository location.
        /// </summary>
        /// <param name="repositoryLocation">The physical repository location.</param>
        /// <param name="studyInstanceUid">The Study Instance UID.</param>
        /// <param name="seriesInstanceUid">The Series Instance UID.</param>
        /// <param name="sopInstanceUid">The SOP Instance UID.</param>
        /// <returns>The validated repository path information.</returns>
        DicomRepositoryPathInfo CreatePathInfo(RepositoryLocation repositoryLocation, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid);

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
        /// Determines whether the specified relative path exists within the
        /// repository location.
        /// </summary>
        /// <param name="repositoryLocation">The physical repository location.</param>
        /// <param name="relativePath">The path relative to the repository root.</param>
        /// <returns>
        /// <c>true</c> if the resolved physical file exists; otherwise, <c>false</c>.
        /// </returns>
        bool Exists(RepositoryLocation repositoryLocation, string relativePath);

        /// <summary>
        /// Gets the absolute path.
        /// </summary>
        /// <param name="relativePath">The relative path.</param>
        /// <returns>The absolute path.</returns>
        string GetAbsolutePath(string relativePath);

        /// <summary>
        /// Resolves and validates an absolute path within the specified repository
        /// location.
        /// </summary>
        /// <param name="repositoryLocation">The physical repository location.</param>
        /// <param name="relativePath">The path relative to the repository root.</param>
        /// <returns>The normalized absolute path.</returns>
        string GetAbsolutePath(RepositoryLocation repositoryLocation, string relativePath);
    }
}