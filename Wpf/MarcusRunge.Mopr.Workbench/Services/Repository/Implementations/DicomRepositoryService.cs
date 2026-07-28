using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Implementations
{
    internal class DicomRepositoryService : CreateableBindableBase<IDicomRepositoryService, DicomRepositoryService, IRepositoryBase>, IDicomRepositoryService
    {
        private IRepositoryBase? _base;
        private IRepositoryBase Base => _base ?? throw new InvalidOperationException("Service has not been initialized.");

        /// <inheritdoc/>
        public DicomRepositoryPathInfo CreatePathInfo(string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            string relativePath = CreateRelativePath(studyInstanceUid, seriesInstanceUid, sopInstanceUid);

            /*
             * This overload remains available only while Import and Repair are
             * migrated to persisted RepositoryLocation records. It uses the
             * existing application-configured path and does not create or
             * persist a repository-location identity.
             */
            return new DicomRepositoryPathInfo
            {
                StudyInstanceUid = studyInstanceUid,
                SeriesInstanceUid = seriesInstanceUid,
                SopInstanceUid = sopInstanceUid,
                RelativePath = relativePath,
                AbsolutePath = GetAbsolutePath(relativePath),
                RepositoryRootPath = GetConfiguredRepositoryRootPath()
            };
        }

        /// <inheritdoc/>
        public DicomRepositoryPathInfo CreatePathInfo(RepositoryLocation repositoryLocation, string studyInstanceUid, string seriesInstanceUid, string sopInstanceUid)
        {
            ArgumentNullException.ThrowIfNull(repositoryLocation);

            if (repositoryLocation.Id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(repositoryLocation), "The repository location must have a persisted positive ID.");
            }

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
        public bool Exists(string relativePath) => File.Exists(GetAbsolutePath(relativePath));

        /// <inheritdoc/>
        public bool Exists(RepositoryLocation repositoryLocation, string relativePath) => File.Exists(GetAbsolutePath(repositoryLocation, relativePath));

        /// <inheritdoc/>
        public string GetAbsolutePath(string relativePath) => ResolveAbsolutePath(GetConfiguredRepositoryRootPath(), relativePath);

        /// <inheritdoc/>
        public string GetAbsolutePath(RepositoryLocation repositoryLocation, string relativePath)
        {
            ArgumentNullException.ThrowIfNull(repositoryLocation);

            string repositoryRootPath = NormalizeRepositoryRootPath(repositoryLocation.RootPath);
            return ResolveAbsolutePath(repositoryRootPath, relativePath);
        }

        protected override void OnCreate(IRepositoryBase @base) => _base = @base;

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
             * Persisted repository paths must be strictly relative and must not
             * contain current- or parent-directory segments. Rejecting these segments
             * before normalization keeps manipulated Persistence values visible
             * instead of silently converting them into apparently valid paths.
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
             * The normalized destination must either equal the repository root or be
             * a child of it. The separator boundary prevents a sibling path with the
             * same textual prefix from being accepted.
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
             * DICOM identifiers are stored as individual path segments. Path
             * separators, rooted values and parent-directory segments would
             * allow the generated path to escape its intended hierarchy.
             */
            if (Path.IsPathFullyQualified(value)                || value is "." or ".."                || value.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0                || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException($"Value '{value}' is not a valid repository path segment.", parameterName);
            }
        }

        private string GetConfiguredRepositoryRootPath()
        {
            string repositoryPath = Base.ApplicationConfiguration?.Repository?.DicomRepositoryPath                ?? throw new InvalidOperationException("The DICOM repository path has not been configured.");

            return NormalizeRepositoryRootPath(repositoryPath);
        }
    }
}