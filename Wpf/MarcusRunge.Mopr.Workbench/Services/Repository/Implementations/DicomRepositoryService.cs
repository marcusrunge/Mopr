using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Implementations
{
    internal class DicomRepositoryService : CreateableBindableBase<IDicomRepositoryService, DicomRepositoryService, IRepositoryBase>, IDicomRepositoryService
    {
        /// <inheritdoc/>
        public DicomRepositoryPathInfo CreatePathInfo(RepositoryLocation repositoryLocation, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            ValidateRepositoryLocation(repositoryLocation);

            string relativePath = CreateRelativePath(studyInstanceUid, seriesInstanceUid, sopInstanceUid);
            string repositoryRootPath = NormalizeRepositoryRootPath(repositoryLocation.RootPath);

            return new DicomRepositoryPathInfo
            {
                StudyInstanceUid = studyInstanceUid,
                SeriesInstanceUid = seriesInstanceUid,
                SopInstanceUid = sopInstanceUid,
                RepositoryLocationId = repositoryLocation.Id,
                RepositoryRootPath = repositoryRootPath,
                RelativePath = relativePath,
                AbsolutePath = ResolveAbsolutePath(repositoryRootPath, relativePath)
            };
        }

        /// <inheritdoc/>
        public string CreateRelativePath(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            ValidatePathSegment(studyInstanceUid, nameof(studyInstanceUid));
            ValidatePathSegment(seriesInstanceUid, nameof(seriesInstanceUid));
            ValidatePathSegment(sopInstanceUid, nameof(sopInstanceUid));

            return Path.Combine(studyInstanceUid, seriesInstanceUid, $"{sopInstanceUid}.dcm");
        }

        /// <inheritdoc/>
        public bool Exists(RepositoryLocation repositoryLocation, string relativePath) => File.Exists(GetAbsolutePath(repositoryLocation, relativePath));

        /// <inheritdoc/>
        public string GetAbsolutePath(RepositoryLocation repositoryLocation, string relativePath)
        {
            ValidateRepositoryLocation(repositoryLocation);

            string repositoryRootPath = NormalizeRepositoryRootPath(repositoryLocation.RootPath);

            return ResolveAbsolutePath(repositoryRootPath, relativePath);
        }

        protected override void OnCreate(IRepositoryBase @base) => ArgumentNullException.ThrowIfNull(@base);

        protected override Task OnCreateAsync(IRepositoryBase @base, CancellationToken cancellationToken) => Task.CompletedTask;

        private static string NormalizeRepositoryRootPath(string? rootPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

            if (!Path.IsPathFullyQualified(rootPath))
            {
                throw new ArgumentException("The repository root path must be an absolute local or UNC path.", nameof(rootPath));
            }

            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
        }

        private static string ResolveAbsolutePath(string repositoryRootPath, string relativePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

            /*
             * Persisted repository paths must be strictly relative and must
             * not contain current- or parent-directory segments. Rejecting
             * these segments before normalization prevents manipulated
             * Persistence values from becoming apparently valid paths.
             */
            if (Path.IsPathFullyQualified(relativePath))
            {
                throw new ArgumentException("The repository path must be relative to its configured repository location.", nameof(relativePath));
            }

            string[] pathSegments = relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

            if (pathSegments.Any(segment => segment is "." or ".."))
            {
                throw new UnauthorizedAccessException($"Relative repository path '{relativePath}' contains a current- or parent-directory segment.");
            }

            string normalizedRootPath = NormalizeRepositoryRootPath(repositoryRootPath);
            string absolutePath = Path.GetFullPath(Path.Combine(normalizedRootPath, relativePath));
            string rootPathWithSeparator = $"{normalizedRootPath}{Path.DirectorySeparatorChar}";

            /*
             * A resolved destination must either equal the repository root or
             * be a child of it. The directory-separator boundary prevents
             * sibling paths with the same textual prefix from being accepted.
             */
            bool isRootPath = string.Equals(absolutePath, normalizedRootPath, StringComparison.OrdinalIgnoreCase);
            bool isChildPath = absolutePath.StartsWith(rootPathWithSeparator, StringComparison.OrdinalIgnoreCase);

            if (!isRootPath && !isChildPath)
            {
                throw new UnauthorizedAccessException($"Relative repository path '{relativePath}' resolves outside repository root '{normalizedRootPath}'.");
            }

            return absolutePath;
        }

        private static void ValidatePathSegment(string value, string parameterName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

            /*
             * DICOM identifiers form individual path segments. Rooted values,
             * path separators and traversal segments would violate the
             * canonical Study-Series-Instance repository hierarchy.
             */
            if (Path.IsPathFullyQualified(value) || value is "." or ".." || value.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0 || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException($"Value '{value}' is not a valid repository path segment.", parameterName);
            }
        }

        private static void ValidateRepositoryLocation(RepositoryLocation repositoryLocation)
        {
            ArgumentNullException.ThrowIfNull(repositoryLocation);

            if (repositoryLocation.Id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(repositoryLocation), "The repository location must have a persisted positive ID.");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(repositoryLocation.RootPath);
        }
    }
}