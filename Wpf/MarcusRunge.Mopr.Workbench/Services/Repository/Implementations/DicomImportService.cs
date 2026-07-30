using FellowOakDicom;
using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Models;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;
using System.Collections.Concurrent;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Implementations
{
    // Concrete IDicomImportService implementation using the
    // CreatableBase lifecycle.
    internal class DicomImportService : CreateableBindableBase<IDicomImportService, DicomImportService, IRepositoryBase>, IDicomImportService
    {
        /*
         * Imports targeting the same canonical physical file are serialized within
         * the MOPR process. Different SOP destinations remain independent and may
         * still be processed concurrently.
         */
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _destinationLocks = new(StringComparer.OrdinalIgnoreCase);
        private IRepositoryBase? _base;
        private IRepositoryBase Base => _base ?? throw new InvalidOperationException("Service has not been initialized.");
        private IDicomImportPersistenceService DicomImportPersistenceService => Persistence.DicomImport ?? throw new InvalidOperationException("The atomic DICOM import Persistence service has not been initialized.");
        private IPersistence Persistence => Base.Persistence ?? throw new InvalidOperationException("Persistence has not been initialized.");
        private IRepository Repository => Base as IRepository ?? throw new InvalidOperationException("The repository base does not implement IRepository.");
        private IRepositoryLocationRepository RepositoryLocationRepository => Persistence.RepositoryLocation ?? throw new InvalidOperationException("The repository-location repository has not been initialized.");

        /// <inheritdoc/>
        public async Task<DicomImportResult> ImportAsync(DicomImportRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.SourcePath);

            if (request.CreatedByUserId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "The ID of the user executing the import must be a positive integer.");
            }

            if (request.RepositoryLocationId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "The repository-location ID must be a positive integer.");
            }

            DicomImportResult result = new();

            if (!Directory.Exists(request.SourcePath))
            {
                result.FailedFiles++;
                result.Errors.Add($"Source path '{request.SourcePath}' does not exist.");
                return result;
            }

            RepositoryLocation? repositoryLocation = await RepositoryLocationRepository.GetByIdAsync(request.RepositoryLocationId, cancellationToken);

            if (repositoryLocation is null)
            {
                result.FailedFiles++;
                result.Errors.Add($"Repository location with ID '{request.RepositoryLocationId}' does not exist.");
                return result;
            }

            if (!repositoryLocation.IsEnabled)
            {
                result.FailedFiles++;
                result.Errors.Add($"Repository location '{repositoryLocation.Id}' is disabled and cannot be used as an import target.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(repositoryLocation.RootPath))
            {
                result.FailedFiles++;
                result.Errors.Add($"Repository location '{repositoryLocation.Id}' has no configured root path.");
                return result;
            }

            /*
             * Path resolution performs the authoritative absolute-path and repository
             * boundary validation. Creating the root here is safe only after the selected
             * persisted location has passed the structural checks above.
             */
            try
            {
                string repositoryRootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryLocation.RootPath));
                Directory.CreateDirectory(repositoryRootPath);
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or UnauthorizedAccessException or IOException)
            {
                result.FailedFiles++;
                result.Errors.Add($"Repository location '{repositoryLocation.Id}' could not be prepared: {exception.Message}");
                return result;
            }

            IList<DicomImportFileInfo> fileInfos = request.SourceType switch
            {
                ImportSourceType.Directory => CreateFileInfos(request.SourcePath),
                ImportSourceType.CdRom => throw new NotSupportedException(),
                ImportSourceType.Dvd => throw new NotSupportedException(),
                ImportSourceType.UsbDrive => throw new NotSupportedException(),
                ImportSourceType.IsoImage => throw new NotSupportedException(),
                ImportSourceType.NetworkShare => throw new NotSupportedException(),
                ImportSourceType.Unknown => throw new ArgumentException("The import source type must not be Unknown.", nameof(request)),
                _ => throw new NotSupportedException($"Import source type '{request.SourceType}' is currently not supported.")
            };

            foreach (DicomImportFileInfo fileInfo in fileInfos)
            {
                result.Files.Add(fileInfo);
            }

            foreach (DicomImportFileInfo fileInfo in result.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!fileInfo.IsImportable)
                {
                    result.SkippedFiles++;
                    continue;
                }

                await ImportFileAsync(fileInfo, result, repositoryLocation, request.AllowOverwrite, request.CreatedByUserId, cancellationToken);
            }

            return result;
        }

        protected override void OnCreate(IRepositoryBase @base) =>
            // Store the repository base required by subsequent
            // import operations.
            _base = @base;

        protected override Task OnCreateAsync(IRepositoryBase @base, CancellationToken cancellationToken) => Task.CompletedTask;

        private static void CompensateFileSystem(DicomImportFileSystemContext fileSystemContext)
        {
            /*
             * Compensation deliberately ignores caller cancellation. Once a physical
             * medical file has been changed, restoring the pre-import state has
             * priority over promptly ending the cancelled operation.
             */
            switch (fileSystemContext.State)
            {
                case DicomImportFileSystemState.None:
                case DicomImportFileSystemState.ExistingIdentical:
                    return;

                case DicomImportFileSystemState.Created:
                    if (File.Exists(fileSystemContext.DestinationPath))
                    {
                        File.Delete(fileSystemContext.DestinationPath);
                    }

                    return;

                case DicomImportFileSystemState.OverwrittenWithBackup:
                    if (string.IsNullOrWhiteSpace(fileSystemContext.BackupPath) || !File.Exists(fileSystemContext.BackupPath))
                    {
                        throw new IOException($"The original repository file backup '{fileSystemContext.BackupPath}' is unavailable and the overwritten file '{fileSystemContext.DestinationPath}' cannot be restored.");
                    }

                    /*
                     * File.Move with overwrite restores the original as one replacement
                     * operation. The currently imported file is removed only as part of
                     * that restoration and is never treated as the authoritative backup.
                     */
                    File.Move(fileSystemContext.BackupPath, fileSystemContext.DestinationPath, true);
                    return;

                default:
                    throw new InvalidOperationException($"Unsupported DICOM import file-system state '{fileSystemContext.State}'.");
            }
        }

        private static async Task CopyToTemporaryFileAsync(string sourcePath, string temporaryPath, CancellationToken cancellationToken)
        {
            const int bufferSize = 81920;

            await using FileStream sourceStream = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using FileStream destinationStream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

            await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken);
            await destinationStream.FlushAsync(cancellationToken);
        }

        private static DicomImportFileInfo CreateFileInfo(string filePath)
        {
            DicomImportFileInfo fileInfo = new()
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath
            };

            DicomFile dicomFile;

            try
            {
                dicomFile = DicomFile.Open(filePath);
            }
            catch
            {
                return fileInfo;
            }

            fileInfo.IsDicomFile = true;
            fileInfo.StudyInstanceUid = dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty);
            fileInfo.SeriesInstanceUid = dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty);
            fileInfo.SopInstanceUid = dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, string.Empty);

            return fileInfo;
        }

        private static IList<DicomImportFileInfo> CreateFileInfos(string sourcePath) => [.. Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories).Select(CreateFileInfo)];

        private static async Task<DicomImportFileSystemContext> CreateFileSystemStateAsync(string sourcePath, string destinationPath, bool allowOverwrite, CancellationToken cancellationToken)
        {
            string temporaryPath = $"{destinationPath}.{Guid.NewGuid():N}.importing";

            try
            {
                DicomImportFileSystemContext fileSystemContext = await PrepareFileSystemStateAsync(
                    sourcePath,
                    destinationPath,
                    temporaryPath,
                    allowOverwrite,
                    cancellationToken);

                /*
                 * A successful preparation must not leave its temporary copy behind.
                 * At this point the copy was either moved to the destination or the
                 * existing destination was found to be identical.
                 */
                DeleteTemporaryFile(temporaryPath);
                return fileSystemContext;
            }
            catch (Exception exception)
            {
                /*
                 * Cleanup is performed outside a finally block so it cannot implicitly
                 * replace the active preparation exception. If cleanup also fails, the
                 * helper preserves both failures in one AggregateException.
                 */
                DeleteTemporaryFile(temporaryPath, exception);
                throw;
            }
        }

        private static void DeleteTemporaryFile(string temporaryPath, Exception? originalException = null)
        {
            if (!File.Exists(temporaryPath))
            {
                return;
            }

            try
            {
                File.Delete(temporaryPath);
            }
            catch (Exception cleanupException)
            {
                if (originalException is null)
                {
                    throw;
                }

                /*
                 * The original preparation failure remains the primary diagnostic
                 * information. The cleanup failure is retained separately instead of
                 * replacing or hiding it.
                 */
                throw new AggregateException(
                    "The DICOM repository file could not be prepared and its temporary import file could not be removed.",
                    originalException,
                    cleanupException);
            }
        }

        private static async Task<bool> FilesAreEqualAsync(string firstPath, string secondPath, CancellationToken cancellationToken)
        {
            FileInfo firstFileInfo = new(firstPath);
            FileInfo secondFileInfo = new(secondPath);

            if (firstFileInfo.Length != secondFileInfo.Length)
            {
                return false;
            }

            const int bufferSize = 81920;

            byte[] firstBuffer = new byte[bufferSize];
            byte[] secondBuffer = new byte[bufferSize];

            await using FileStream firstStream = new(firstPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using FileStream secondStream = new(secondPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

            while (true)
            {
                int firstBytesRead = await firstStream.ReadAsync(firstBuffer, cancellationToken);
                int secondBytesRead = await secondStream.ReadAsync(secondBuffer, cancellationToken);

                if (firstBytesRead != secondBytesRead)
                {
                    return false;
                }

                if (firstBytesRead == 0)
                {
                    return true;
                }

                if (!firstBuffer.AsSpan(0, firstBytesRead).SequenceEqual(secondBuffer.AsSpan(0, secondBytesRead)))
                {
                    return false;
                }
            }
        }

        private static void FinalizeFileSystem(DicomImportFileSystemContext fileSystemContext)
        {
            if (fileSystemContext.State != DicomImportFileSystemState.OverwrittenWithBackup)
            {
                return;
            }

            /*
             * The original medical file is retained until Persistence has committed.
             * Only a completely successful database operation permits its backup to be
             * deleted.
             */
            if (!string.IsNullOrWhiteSpace(fileSystemContext.BackupPath) && File.Exists(fileSystemContext.BackupPath))
            {
                File.Delete(fileSystemContext.BackupPath);
            }
        }

        private static async Task<DicomImportFileSystemContext> PrepareFileSystemStateAsync(string sourcePath, string destinationPath, string temporaryPath, bool allowOverwrite, CancellationToken cancellationToken)
        {
            DicomImportFileSystemContext fileSystemContext = new()
            {
                DestinationPath = destinationPath,
                State = DicomImportFileSystemState.None
            };

            string? destinationDirectory = Path.GetDirectoryName(destinationPath);

            if (string.IsNullOrWhiteSpace(destinationDirectory))
            {
                throw new InvalidOperationException("The repository destination directory could not be determined.");
            }

            Directory.CreateDirectory(destinationDirectory);

            if (File.Exists(destinationPath))
            {
                bool filesAreEqual = await FilesAreEqualAsync(sourcePath, destinationPath, cancellationToken);

                if (filesAreEqual)
                {
                    fileSystemContext.State = DicomImportFileSystemState.ExistingIdentical;
                    return fileSystemContext;
                }

                if (!allowOverwrite)
                {
                    throw new IOException($"A different file already exists at repository path '{destinationPath}'.");
                }
            }

            /*
             * The new content is copied completely before the destination is created
             * or the original is moved. Cancellation during copying therefore leaves
             * the authoritative destination unchanged.
             */
            await CopyToTemporaryFileAsync(sourcePath, temporaryPath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(destinationPath))
            {
                File.Move(temporaryPath, destinationPath);
                fileSystemContext.State = DicomImportFileSystemState.Created;
                return fileSystemContext;
            }

            /*
             * The destination is checked again after copying because another operation
             * may have created or changed it while the source was transferred to the
             * temporary file. The current physical state, not the earlier observation,
             * determines whether replacement is permitted.
             */
            bool currentDestinationIsIdentical = await FilesAreEqualAsync(temporaryPath, destinationPath, cancellationToken);

            if (currentDestinationIsIdentical)
            {
                fileSystemContext.State = DicomImportFileSystemState.ExistingIdentical;
                return fileSystemContext;
            }

            if (!allowOverwrite)
            {
                throw new IOException($"A different file already exists at repository path '{destinationPath}'.");
            }

            fileSystemContext.BackupPath = $"{destinationPath}.{Guid.NewGuid():N}.backup";
            File.Move(destinationPath, fileSystemContext.BackupPath);

            try
            {
                File.Move(temporaryPath, destinationPath);
                fileSystemContext.State = DicomImportFileSystemState.OverwrittenWithBackup;
                return fileSystemContext;
            }
            catch (Exception replacementException)
            {
                /*
                 * Failure after moving the original to backup must restore the
                 * authoritative medical file before control returns to the caller.
                 */
                try
                {
                    File.Move(fileSystemContext.BackupPath, destinationPath);
                }
                catch (Exception restorationException)
                {
                    throw new AggregateException("The repository destination could not be replaced and its original file could not be restored.", replacementException, restorationException);
                }

                throw;
            }
        }

        private async Task ImportFileAsync(DicomImportFileInfo fileInfo, DicomImportResult result, RepositoryLocation repositoryLocation, bool allowOverwrite, int createdByUserId, CancellationToken cancellationToken)
        {
            DicomRepositoryPathInfo pathInfo;

            try
            {
                pathInfo = Repository.RepositoryService!.CreatePathInfo(
                    repositoryLocation,
                    fileInfo.StudyInstanceUid,
                    fileInfo.SeriesInstanceUid,
                    fileInfo.SopInstanceUid);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                result.FailedFiles++;
                result.Errors.Add($"File '{fileInfo.FilePath}' could not be imported: {exception}");
                return;
            }

            /*
             * The semaphore covers physical preparation, Persistence, finalization and
             * compensation. A second import of the same destination cannot observe or
             * modify an intermediate state created by this operation.
             */
            SemaphoreSlim destinationLock = _destinationLocks.GetOrAdd(pathInfo.AbsolutePath, static _ => new SemaphoreSlim(1, 1));
            await destinationLock.WaitAsync(cancellationToken);

            try
            {
                await ImportFileUnderLockAsync(fileInfo, result, repositoryLocation, pathInfo, allowOverwrite, createdByUserId, cancellationToken);
            }
            finally
            {
                destinationLock.Release();
            }
        }

        private async Task ImportFileUnderLockAsync(DicomImportFileInfo fileInfo, DicomImportResult result, RepositoryLocation repositoryLocation, DicomRepositoryPathInfo pathInfo, bool allowOverwrite, int createdByUserId, CancellationToken cancellationToken)
        {
            DicomImportFileSystemContext fileSystemContext = new();
            bool persistenceCommitted = false;

            try
            {
                /*
                 * CreatePathInfo already performed the authoritative repository-boundary
                 * validation. Every temporary and backup file is derived from this
                 * validated destination and remains in the same repository location.
                 */
                fileSystemContext = await CreateFileSystemStateAsync(fileInfo.FilePath, pathInfo.AbsolutePath, allowOverwrite, cancellationToken);
                fileInfo.RelativeRepositoryPath = pathInfo.RelativePath;

                await DicomImportPersistenceService.PersistAsync(new DicomImportPersistenceRequest
                {
                    CreatedByUserId = createdByUserId,
                    RepositoryLocationId = repositoryLocation.Id,
                    StudyInstanceUid = fileInfo.StudyInstanceUid,
                    SeriesInstanceUid = fileInfo.SeriesInstanceUid,
                    SopInstanceUid = fileInfo.SopInstanceUid,
                    RelativeFilePath = pathInfo.RelativePath
                }, cancellationToken);

                persistenceCommitted = true;

                /*
                 * Finalization occurs only after Persistence has committed. A backup
                 * deletion failure is reported while retaining the backup as the
                 * recoverable original medical file.
                 */
                FinalizeFileSystem(fileSystemContext);

                if (fileSystemContext.State == DicomImportFileSystemState.ExistingIdentical)
                {
                    result.SkippedFiles++;
                }
                else
                {
                    result.ImportedFiles++;
                }
            }
            catch (Exception exception)
            {
                if (!persistenceCommitted)
                {
                    try
                    {
                        CompensateFileSystem(fileSystemContext);
                    }
                    catch (Exception compensationException)
                    {
                        /*
                         * A failed compensation is not a normal per-file import error.
                         * The repository may no longer match Persistence, so callers
                         * must receive both failures and escalate the condition.
                         */
                        throw new AggregateException($"File '{fileInfo.FilePath}' could not be imported and its repository file-system state could not be restored.", exception, compensationException);
                    }
                }

                if (exception is OperationCanceledException)
                {
                    throw;
                }

                result.FailedFiles++;
                result.Errors.Add($"File '{fileInfo.FilePath}' could not be imported: {exception}");
            }
        }
    }
}