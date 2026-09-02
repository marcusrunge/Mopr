using MarcusRunge.Mopr.Workbench.Contracts.Application.Configuration;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Application.Configuration
{
    /// <summary>
    /// Validates repository locations through controlled Windows file-system operations.
    /// </summary>
    internal sealed class RepositoryLocationValidationService : IRepositoryLocationValidationService
    {
        /// <inheritdoc/>
        public Task<RepositoryLocationValidationResult> ValidateAsync(string directoryPath, CancellationToken cancellationToken = default) =>
            Task.Run(() => Validate(directoryPath, cancellationToken), cancellationToken);

        private static RepositoryLocationValidationResult Validate(string directoryPath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return new RepositoryLocationValidationResult();
            }

            string normalizedPath;

            try
            {
                normalizedPath = Path.GetFullPath(directoryPath);
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return new RepositoryLocationValidationResult();
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(normalizedPath))
            {
                return new RepositoryLocationValidationResult
                {
                    NormalizedPath = normalizedPath
                };
            }

            var isReadable = CanRead(normalizedPath, cancellationToken);
            var isWritable = isReadable && CanWrite(normalizedPath, cancellationToken);

            return new RepositoryLocationValidationResult
            {
                Exists = true,
                IsReadable = isReadable,
                IsWritable = isWritable,
                NormalizedPath = normalizedPath
            };
        }

        private static bool CanRead(string directoryPath, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var enumerator = Directory.EnumerateFileSystemEntries(directoryPath).GetEnumerator();
                _ = enumerator.MoveNext();

                cancellationToken.ThrowIfCancellationRequested();
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or DirectoryNotFoundException)
            {
                return false;
            }
        }

        private static bool CanWrite(string directoryPath, CancellationToken cancellationToken)
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

                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or DirectoryNotFoundException or NotSupportedException or PathTooLongException)
            {
                return false;
            }
            finally
            {
                TryDeleteValidationFile(validationFilePath);
            }
        }

        private static void TryDeleteValidationFile(string validationFilePath)
        {
            try
            {
                if (File.Exists(validationFilePath))
                {
                    File.Delete(validationFilePath);
                }
            }
            catch
            {
                // Cleanup failure must not hide the actual repository-location validation result.
            }
        }
    }
}