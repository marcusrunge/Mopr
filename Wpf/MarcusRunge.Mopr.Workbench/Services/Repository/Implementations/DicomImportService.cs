using FellowOakDicom;
using MarcusRunge.Base;
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
        private IRepository Repository => Base as IRepository ?? throw new InvalidOperationException("The repository base does not implement IRepository.");

        /// <inheritdoc/>
        public async Task<DicomImportResult> ImportAsync(DicomImportRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.SourcePath);

            DicomImportResult result = new();

            if (!Directory.Exists(request.SourcePath))
            {
                result.FailedFiles++;

                result.Errors.Add($"Source path '{request.SourcePath}' does not exist.");

                return result;
            }

            IList<DicomImportFileInfo> fileInfos =
                request.SourceType switch
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

                await ImportFileAsync(fileInfo, result, request.AllowOverwrite, cancellationToken);
            }

            return result;
        }

        protected override void OnCreate(
            IRepositoryBase @base) =>
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

        private async Task ImportFileAsync(DicomImportFileInfo fileInfo, DicomImportResult result, bool allowOverwrite, CancellationToken cancellationToken)
        {
            try
            {
                DicomRepositoryPathInfo pathInfo = Repository.RepositoryService!.CreatePathInfo(fileInfo.StudyInstanceUid, fileInfo.SeriesInstanceUid, fileInfo.SopInstanceUid);
                string? destinationDirectory = Path.GetDirectoryName(pathInfo.AbsolutePath);

                if (string.IsNullOrWhiteSpace(destinationDirectory))
                {
                    throw new InvalidOperationException("The repository destination directory could not be determined.");
                }

                Directory.CreateDirectory(destinationDirectory);

                await CopyFileAsync(fileInfo.FilePath, pathInfo.AbsolutePath, allowOverwrite, cancellationToken);

                fileInfo.RelativeRepositoryPath = pathInfo.RelativePath;

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