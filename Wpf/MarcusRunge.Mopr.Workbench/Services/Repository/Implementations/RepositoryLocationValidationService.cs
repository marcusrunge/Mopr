using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Implementations
{
    /// <summary>
    /// Validates repository locations through controlled file-system operations.
    /// </summary>
    internal sealed class RepositoryLocationValidationService : CreateableBindableBase<IRepositoryLocationValidationService, RepositoryLocationValidationService, IRepositoryBase>, IRepositoryLocationValidationService
    {
        /// <inheritdoc/>
        public Task<RepositoryLocationValidationResult> ValidateAsync(string directoryPath, CancellationToken cancellationToken = default) => Task.Run(() => Validate(directoryPath, cancellationToken), cancellationToken);

        /// <inheritdoc/>
        protected override void OnCreate(IRepositoryBase @base) => ArgumentNullException.ThrowIfNull(@base);

        /// <inheritdoc/>
        protected override Task OnCreateAsync(IRepositoryBase @base, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(@base);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        private static RepositoryLocationValidationFailureKind ClassifyAccessFailure(Exception exception, bool isNetworkLocation)
        {
            if (exception is UnauthorizedAccessException) return RepositoryLocationValidationFailureKind.AccessDenied;

            // File-system APIs report missing local paths and unreachable network roots
            // through overlapping exception types. The path kind therefore determines
            // whether absence is a local configuration error or temporary unavailability.
            if (exception is FileNotFoundException or DirectoryNotFoundException) return isNetworkLocation ? RepositoryLocationValidationFailureKind.Unavailable : RepositoryLocationValidationFailureKind.NotFound;
            if (exception is IOException) return RepositoryLocationValidationFailureKind.Unavailable;
            return RepositoryLocationValidationFailureKind.InvalidPath;
        }

        private static RepositoryLocationValidationFailureKind GetExistenceFailure(string directoryPath, bool isNetworkLocation, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _ = File.GetAttributes(directoryPath);
                cancellationToken.ThrowIfCancellationRequested();
                return RepositoryLocationValidationFailureKind.None;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or DirectoryNotFoundException)
            {
                return ClassifyAccessFailure(exception, isNetworkLocation);
            }
        }

        private static RepositoryLocationValidationFailureKind GetReadFailure(string directoryPath, bool isNetworkLocation, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var enumerator = Directory.EnumerateFileSystemEntries(directoryPath).GetEnumerator();
                _ = enumerator.MoveNext();
                cancellationToken.ThrowIfCancellationRequested();
                return RepositoryLocationValidationFailureKind.None;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or DirectoryNotFoundException)
            {
                return ClassifyAccessFailure(exception, isNetworkLocation);
            }
        }

        private static RepositoryLocationValidationFailureKind GetWriteFailure(string directoryPath, bool isNetworkLocation, CancellationToken cancellationToken)
        {
            var validationFilePath = Path.Combine(directoryPath, $".mopr-write-test-{Guid.NewGuid():N}.tmp");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var stream = new FileStream(validationFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.WriteThrough))
                {
                    stream.WriteByte(0);
                    stream.Flush(flushToDisk: true);
                }

                cancellationToken.ThrowIfCancellationRequested();
                File.Delete(validationFilePath);
                return RepositoryLocationValidationFailureKind.None;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                return RepositoryLocationValidationFailureKind.AccessDenied;
            }
            catch (Exception exception) when (exception is IOException or DirectoryNotFoundException)
            {
                return isNetworkLocation ? RepositoryLocationValidationFailureKind.Unavailable : RepositoryLocationValidationFailureKind.ReadOnly;
            }
            catch (Exception exception) when (exception is NotSupportedException or PathTooLongException)
            {
                return RepositoryLocationValidationFailureKind.InvalidPath;
            }
            finally
            {
                TryDeleteValidationFile(validationFilePath);
            }
        }

        private static bool IsNetworkLocation(string directoryPath)
        {
            if (directoryPath.StartsWith(@"\\", StringComparison.Ordinal)) return true;

            try
            {
                var rootPath = Path.GetPathRoot(directoryPath);
                return !string.IsNullOrWhiteSpace(rootPath) && new DriveInfo(rootPath).DriveType == DriveType.Network;
            }
            catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static void TryDeleteValidationFile(string validationFilePath)
        {
            try
            {
                if (File.Exists(validationFilePath)) File.Delete(validationFilePath);
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or NotSupportedException)
            {
                // Validation cleanup is best effort and must not replace the original result.
            }
        }

        private static RepositoryLocationValidationResult Validate(string directoryPath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(directoryPath)) return new RepositoryLocationValidationResult { FailureKind = RepositoryLocationValidationFailureKind.InvalidPath };

            // Classify the caller-supplied path before normalization. Normalization and
            // platform-specific file-system handling must not erase UNC semantics.
            var isNetworkLocation = IsNetworkLocation(directoryPath);
            string normalizedPath;

            try
            {
                normalizedPath = Path.GetFullPath(directoryPath);
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return new RepositoryLocationValidationResult { FailureKind = RepositoryLocationValidationFailureKind.InvalidPath };
            }

            var existenceFailure = GetExistenceFailure(normalizedPath, isNetworkLocation, cancellationToken);
            if (existenceFailure != RepositoryLocationValidationFailureKind.None) return new RepositoryLocationValidationResult { FailureKind = existenceFailure, NormalizedPath = normalizedPath };

            var readFailure = GetReadFailure(normalizedPath, isNetworkLocation, cancellationToken);
            if (readFailure != RepositoryLocationValidationFailureKind.None) return new RepositoryLocationValidationResult { Exists = true, FailureKind = readFailure, NormalizedPath = normalizedPath };

            var writeFailure = GetWriteFailure(normalizedPath, isNetworkLocation, cancellationToken);
            return new RepositoryLocationValidationResult { Exists = true, FailureKind = writeFailure, IsReadable = true, IsWritable = writeFailure == RepositoryLocationValidationFailureKind.None, NormalizedPath = normalizedPath };
        }
    }
}
