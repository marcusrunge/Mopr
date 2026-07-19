using FellowOakDicom;
using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Repository.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Repository.Enums;
using MarcusRunge.Mopr.Workbench.Services.Repository.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Implementations
{
    // Concrete IDicomImportService implementation using the CreatableBase lifecycle (sync create + optional async init).
    internal class DicomImportService : CreateableBindableBase<IDicomImportService, DicomImportService, IRepositoryBase>, IDicomImportService
    {
        private IRepositoryBase? _base;

        private IRepositoryBase Base => _base ?? throw new InvalidOperationException("Service has not been initialized.");

        /// <inheritdoc/>
        public async Task<DicomImportResult> ImportAsync(DicomImportRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.SourcePath);

            DicomImportResult result = new();

            if (!Directory.Exists(request.SourcePath))
            {
                result.FailedFiles++; result.Errors.Add($"Source path '{request.SourcePath}' does not exist.");
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

            await Task.CompletedTask;
            return result;
        }

        protected override void OnCreate(IRepositoryBase @base) =>
            // Store the repository base required by subsequent import operations.
            _base = @base;

        protected override Task OnCreateAsync(IRepositoryBase @base, CancellationToken cancellationToken) =>
            /*What happens here:
              - This is the asynchronous initialization hook that runs after the instance exists.
              - It is invoked by the base lifecycle to perform potentially expensive/IO work without blocking creation.
              - Returning Task.CompletedTask signals: "no async initialization required" for ServiceB.
              - The provided cancellationToken is not used here because there is nothing to cancel. */
            Task.CompletedTask;

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
    }
}