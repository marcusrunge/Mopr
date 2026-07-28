using FellowOakDicom;
using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Entities;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Implementations
{
    // Concrete IDicomImportService implementation using the
    // CreatableBase lifecycle.
    internal class DicomImportService : CreateableBindableBase<IDicomImportService, DicomImportService, IRepositoryBase>, IDicomImportService
    {
        private IRepositoryBase? _base;

        private IRepositoryBase Base => _base ?? throw new InvalidOperationException("Service has not been initialized.");
        private IInstanceRepository InstanceRepository => Persistence.Instance ?? throw new InvalidOperationException("The instance repository has not been initialized.");
        private IPersistence Persistence => Base.Persistence ?? throw new InvalidOperationException("Persistence has not been initialized.");
        private IRepository Repository => Base as IRepository ?? throw new InvalidOperationException("The repository base does not implement IRepository.");
        private IRepositoryLocationRepository RepositoryLocationRepository => Persistence.RepositoryLocation ?? throw new InvalidOperationException("The repository-location repository has not been initialized.");
        private ISeriesRepository SeriesRepository => Persistence.Series ?? throw new InvalidOperationException("The series repository has not been initialized.");
        private IStudyRepository StudyRepository => Persistence.Study ?? throw new InvalidOperationException("The study repository has not been initialized.");
        private IUserRepository UserRepository => Persistence.User ?? throw new InvalidOperationException("The user repository has not been initialized.");

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
            string repositoryRootPath;

            try
            {
                repositoryRootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryLocation.RootPath));
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
                _ => throw new NotSupportedException($"Import source type '{request.SourceType}' is currently not supported."),
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

        private static async Task CopyFileAsync(string sourcePath, string destinationPath, bool allowOverwrite, CancellationToken cancellationToken)
        {
            const int bufferSize = 81920;
            string temporaryPath = $"{destinationPath}.{Guid.NewGuid():N}.importing";

            try
            {
                await using (FileStream sourceStream = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await using FileStream destinationStream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken);
                    await destinationStream.FlushAsync(cancellationToken);
                }

                File.Move(temporaryPath, destinationPath, allowOverwrite);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
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

        private async Task PersistFileAsync(DicomImportFileInfo fileInfo, RepositoryLocation repositoryLocation, int createdByUserId, CancellationToken cancellationToken)
        {
            _ = await UserRepository.GetByIdAsync(createdByUserId, cancellationToken) ?? throw new InvalidOperationException($"The user with ID '{createdByUserId}' does not exist.");
            Study? study = await StudyRepository.GetByStudyInstanceUidAsync(fileInfo.StudyInstanceUid, cancellationToken);

            if (study is null)
            {
                study = new Study
                {
                    StudyInstanceUid = fileInfo.StudyInstanceUid,
                    CreatedByUserId = createdByUserId
                };

                await StudyRepository.AddAsync(study, cancellationToken);
            }

            Series? series = await SeriesRepository.GetBySeriesInstanceUidAsync(fileInfo.SeriesInstanceUid, cancellationToken);

            if (series is null)
            {
                series = new Series
                {
                    SeriesInstanceUid = fileInfo.SeriesInstanceUid,
                    StudyId = study.Id,
                    CreatedByUserId = createdByUserId
                };

                await SeriesRepository.AddAsync(series, cancellationToken);
            }
            else if (series.StudyId != study.Id)
            {
                throw new InvalidOperationException($"Series '{fileInfo.SeriesInstanceUid}' belongs to a different study.");
            }

            Instance? instance = await InstanceRepository.GetBySopInstanceUidAsync(fileInfo.SopInstanceUid, cancellationToken);

            if (instance is null)
            {
                instance = new Instance
                {
                    SopInstanceUid = fileInfo.SopInstanceUid,
                    RelativeFilePath = fileInfo.RelativeRepositoryPath,
                    RepositoryLocationId = repositoryLocation.Id,
                    SeriesId = series.Id,
                    CreatedByUserId = createdByUserId
                };

                await InstanceRepository.AddAsync(instance, cancellationToken);

                return;
            }

            if (instance.SeriesId != series.Id)
            {
                throw new InvalidOperationException($"Instance '{fileInfo.SopInstanceUid}' belongs to a different series.");
            }

            /*
             * An existing SOP instance is already assigned to one physical repository
             * location. Import must not silently move or duplicate that assignment into a
             * different location because Persistence would no longer identify one
             * authoritative physical file.
             */
            if (instance.RepositoryLocationId != repositoryLocation.Id)
            {
                throw new InvalidOperationException($"Instance '{fileInfo.SopInstanceUid}' belongs to repository location '{instance.RepositoryLocationId}' and cannot be imported into repository location '{repositoryLocation.Id}'.");
            }

            if (instance.RelativeFilePath != fileInfo.RelativeRepositoryPath)
            {
                instance.RelativeFilePath = fileInfo.RelativeRepositoryPath;
                instance.ModifiedAtUtc = DateTime.UtcNow;
                instance.ModifiedByUserId = createdByUserId;

                await InstanceRepository.UpdateAsync(instance, cancellationToken);
            }
        }

        private async Task ImportFileAsync(DicomImportFileInfo fileInfo, DicomImportResult result, RepositoryLocation repositoryLocation, bool allowOverwrite, int createdByUserId, CancellationToken cancellationToken)
        {
            try
            {
                DicomRepositoryPathInfo pathInfo = Repository.RepositoryService!.CreatePathInfo(repositoryLocation, fileInfo.StudyInstanceUid, fileInfo.SeriesInstanceUid, fileInfo.SopInstanceUid);

                string? destinationDirectory = Path.GetDirectoryName(pathInfo.AbsolutePath);

                if (string.IsNullOrWhiteSpace(destinationDirectory))
                {
                    throw new InvalidOperationException("The repository destination directory could not be determined.");
                }

                Directory.CreateDirectory(destinationDirectory);

                if (File.Exists(pathInfo.AbsolutePath) && !allowOverwrite)
                {
                    bool filesAreEqual = await FilesAreEqualAsync(fileInfo.FilePath, pathInfo.AbsolutePath, cancellationToken);

                    if (!filesAreEqual)
                    {
                        throw new IOException($"A different file already exists at repository path '{pathInfo.RelativePath}'.");
                    }

                    fileInfo.RelativeRepositoryPath = pathInfo.RelativePath;

                    await PersistFileAsync(fileInfo, repositoryLocation, createdByUserId, cancellationToken);

                    result.SkippedFiles++;
                    return;
                }

                await CopyFileAsync(fileInfo.FilePath, pathInfo.AbsolutePath, allowOverwrite, cancellationToken);

                fileInfo.RelativeRepositoryPath = pathInfo.RelativePath;

                await PersistFileAsync(fileInfo, repositoryLocation, createdByUserId, cancellationToken);

                result.ImportedFiles++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                result.FailedFiles++;
                result.Errors.Add($"File '{fileInfo.FilePath}' could not be imported: {exception.Message}");
            }
        }
    }
}