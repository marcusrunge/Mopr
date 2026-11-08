using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Core.Properties;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Implementations.Imaging
{
    internal sealed class ImagingStudyService : CreateableBindableBase<IImagingStudyService, ImagingStudyService, IImagingServiceBase>, IImagingStudyService
    {
        private readonly List<SeriesInfo> _currentSeries = [];
        private readonly Dictionary<string, IReadOnlyList<DicomFileMetadata>> _seriesDicomMetadata = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IReadOnlyList<string>> _seriesFiles = new(StringComparer.OrdinalIgnoreCase);
        private IImagingServiceBase? _base;
        private StudyInfo? _currentStudy;

        private ImagingFolderScanSummary? _lastScanSummary;

        public event EventHandler<ImagingStudyLoadedEventArgs>? StudyLoaded;

        public IReadOnlyList<SeriesInfo> CurrentSeries => _currentSeries;
        public StudyInfo? CurrentStudy => _currentStudy;

        public ImagingFolderScanSummary? LastScanSummary => _lastScanSummary;

        public void Clear()
        {
            _currentStudy = null;
            _currentSeries.Clear();
            _seriesFiles.Clear();
            _seriesDicomMetadata.Clear();
            _lastScanSummary = null;
            RaiseStudyLoaded();
        }

        public IReadOnlyList<DicomFileMetadata> GetDicomMetadataForSeries(string seriesId)
        {
            if (string.IsNullOrWhiteSpace(seriesId))
            {
                return [];
            }

            return _seriesDicomMetadata.TryGetValue(seriesId, out var metadata) ? metadata : [];
        }

        public IReadOnlyList<string> GetFilesForSeries(string seriesId) => string.IsNullOrWhiteSpace(seriesId) ? Array.Empty<string>() : _seriesFiles.TryGetValue(seriesId, out var files) ? files : Array.Empty<string>();

        public DicomFileMetadata? GetFirstDicomMetadataForSeries(string seriesId)
        {
            var metadata = GetDicomMetadataForSeries(seriesId);
            return metadata.Count > 0 ? metadata[0] : null;
        }

        public Task LoadStudyFromFilesAsync(string studyInstanceUid, string seriesInstanceUid, IReadOnlyList<string> files, IProgress<ImagingStudyLoadProgress>? progress = null, CancellationToken cancellationToken = default) => LoadStudyFromSeriesAsync(studyInstanceUid, new[] { new ImagingSeriesLoadRequest(seriesInstanceUid, files) }, progress, cancellationToken);

        public async Task LoadStudyFromSeriesAsync(string studyInstanceUid, IReadOnlyList<ImagingSeriesLoadRequest> series, IProgress<ImagingStudyLoadProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(studyInstanceUid)) throw new ArgumentException("The study instance UID must not be empty.", nameof(studyInstanceUid));
            if (series is null) throw new ArgumentNullException(nameof(series));
            if (series.Count == 0) throw new ArgumentException("At least one validated DICOM series is required.", nameof(series));
            cancellationToken.ThrowIfCancellationRequested();

            var metadataService = _base?.CoreBase?.Dicom?.MetadataService ?? throw new InvalidOperationException("The DICOM metadata service has not been initialized.");
            Dictionary<string, IReadOnlyList<string>> validatedFiles = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, IReadOnlyList<DicomFileMetadata>> validatedMetadata = new(StringComparer.OrdinalIgnoreCase);
            List<SeriesInfo> validatedSeries = new(series.Count);
            HashSet<string> seriesInstanceUids = new(StringComparer.Ordinal);
            DicomFileMetadata? firstStudyMetadata = null;
            var totalFiles = series.Sum(item => item.Files?.Count ?? 0);
            var processedFiles = 0;
            progress?.Report(new ImagingStudyLoadProgress(message: Resources.ImagingStudyService_DicomMetadataReadProgress, processedFiles: 0, totalFiles: totalFiles));

            foreach (ImagingSeriesLoadRequest request in series)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(request.SeriesInstanceUid)) throw new ArgumentException("A series instance UID must not be empty.", nameof(series));
                if (request.Files is null || request.Files.Count == 0) throw new ArgumentException("Every selected series must contain at least one validated DICOM file.", nameof(series));
                if (!seriesInstanceUids.Add(request.SeriesInstanceUid)) throw new InvalidDataException($"Series instance UID '{request.SeriesInstanceUid}' occurs more than once in the imaging request.");

                List<DicomFileMetadata> metadata = new(request.Files.Count);
                for (var index = 0; index < request.Files.Count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var filePath = request.Files[index];
                    if (string.IsNullOrWhiteSpace(filePath) || !metadataService.IsDicomFile(filePath)) throw new InvalidDataException($"Validated repository file at index '{index}' is not a readable DICOM file.");
                    DicomFileMetadata? item = await metadataService.ReadMetadataAsync(filePath, cancellationToken).ConfigureAwait(false) ?? throw new InvalidDataException($"DICOM metadata could not be read for validated repository file at index '{index}'.");
                    if (!string.Equals(item.StudyInstanceUid, studyInstanceUid, StringComparison.Ordinal) || !string.Equals(item.SeriesInstanceUid, request.SeriesInstanceUid, StringComparison.Ordinal)) throw new InvalidDataException($"Validated repository file at index '{index}' does not belong to the selected study and series.");
                    metadata.Add(item);
                    firstStudyMetadata ??= item;
                    processedFiles++;
                    progress?.Report(new ImagingStudyLoadProgress(message: Resources.ImagingStudyService_DicomMetadataReadProgress, processedFiles: processedFiles, totalFiles: totalFiles));
                }

                DicomFileMetadata first = metadata[0];
                var seriesId = CreateSeriesId(studyInstanceUid, request.SeriesInstanceUid, first.SeriesNumber ?? validatedSeries.Count + 1);
                var seriesName = string.IsNullOrWhiteSpace(first.SeriesDescription) ? first.Modality ?? request.SeriesInstanceUid : first.SeriesDescription;
                validatedSeries.Add(new SeriesInfo(id: seriesId, modality: first.Modality ?? "DICOM", name: seriesName, description: first.SeriesDescription ?? string.Empty, imageCount: request.Files.Count, studyId: studyInstanceUid, seriesNumber: first.SeriesNumber));
                validatedFiles[seriesId] = [.. request.Files];
                validatedMetadata[seriesId] = metadata;
            }

            if (firstStudyMetadata is null) throw new InvalidDataException("No DICOM metadata was resolved for the selected study.");
            var studyName = string.IsNullOrWhiteSpace(firstStudyMetadata.StudyDescription) ? firstStudyMetadata.PatientName ?? studyInstanceUid : firstStudyMetadata.StudyDescription;
            var study = new StudyInfo(id: studyInstanceUid, name: studyName, description: firstStudyMetadata.StudyDescription ?? string.Empty, studyDate: firstStudyMetadata.StudyDate, patientDisplayName: firstStudyMetadata.PatientName, accessionNumber: firstStudyMetadata.AccessionNumber);

            _seriesFiles.Clear();
            foreach (var item in validatedFiles) _seriesFiles[item.Key] = item.Value;
            _seriesDicomMetadata.Clear();
            foreach (var item in validatedMetadata) _seriesDicomMetadata[item.Key] = item.Value;
            _lastScanSummary = null;
            ApplyStudy(study, validatedSeries);
            progress?.Report(new ImagingStudyLoadProgress(message: Resources.ImagingStudyService_ScanCompletedProgress, processedFiles: totalFiles, totalFiles: totalFiles));
        }

        public async Task LoadStudyFromFolderAsync(string folderPath, IProgress<ImagingStudyLoadProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                progress?.Report(new ImagingStudyLoadProgress(message: Resources.ImagingStudyService_LoadCanceled, processedFiles: 0, totalFiles: 0));

                return;
            }

            FolderScanResult scanResult;

            try
            {
                scanResult = await ScanFolderAsync(folderPath, progress, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                progress?.Report(new ImagingStudyLoadProgress(message: Resources.ImagingStudyService_LoadCanceled, processedFiles: 0, totalFiles: 0));

                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                progress?.Report(new ImagingStudyLoadProgress(message: Resources.ImagingStudyService_LoadCanceled, processedFiles: 0, totalFiles: 0));

                return;
            }

            var study = new StudyInfo(id: scanResult.StudyId, name: scanResult.FolderName, description: scanResult.FolderPath);

            _lastScanSummary = scanResult.Summary;

            _seriesFiles.Clear();

            foreach (var item in scanResult.SeriesFiles)
            {
                _seriesFiles[item.Key] = item.Value;
            }

            _seriesDicomMetadata.Clear();

            foreach (var item in scanResult.SeriesDicomMetadata)
            {
                _seriesDicomMetadata[item.Key] = item.Value;
            }
            ApplyStudy(study, scanResult.Series);
        }

        protected override void OnCreate(IImagingServiceBase @base) => _base = @base;

        protected override Task OnCreateAsync(IImagingServiceBase @base, CancellationToken cancellationToken)
        {
            _base = @base;
            return Task.CompletedTask;
        }

        private static string CreateSeriesId(string studyId, string? seriesInstanceUid, int seriesNumber) => !string.IsNullOrWhiteSpace(seriesInstanceUid) ? $"{studyId}-series-{seriesInstanceUid}" : $"{studyId}-series-{seriesNumber}";

        private static string GetSafeFolderName(string folderPath)
        {
            var trimmedPath = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var folderName = Path.GetFileName(trimmedPath);

            return string.IsNullOrWhiteSpace(folderName) ? folderPath : folderName;
        }

        private static bool IsDicomCandidate(string filePath)
        {
            var extension = Path.GetExtension(filePath);

            return string.Equals(extension, ".dcm", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".dicom", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(extension);
        }

        private static bool IsImageCandidate(string filePath)
        {
            var extension = Path.GetExtension(filePath);

            return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".tif", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".tiff", StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyStudy(StudyInfo study, IReadOnlyList<SeriesInfo> series)
        {
            _currentStudy = study;

            _currentSeries.Clear();
            _currentSeries.AddRange(series);

            RaiseStudyLoaded();
        }

        private void RaiseStudyLoaded() => StudyLoaded?.Invoke(this, new ImagingStudyLoadedEventArgs(_currentStudy, [.. _currentSeries], _lastScanSummary));

        private async Task<FolderScanResult> ScanFolderAsync(string folderPath, IProgress<ImagingStudyLoadProgress>? progress, CancellationToken cancellationToken)
        {
            progress?.Report(new ImagingStudyLoadProgress(message: Resources.ImagingStudyService_FilesSearchProgress, processedFiles: 0, totalFiles: 0));

            var folderName = GetSafeFolderName(folderPath);

            var allFiles = await Task.Run(() =>
            {
                var files = new List<string>();

                foreach (var file in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    files.Add(file);
                }

                return files;
            }, cancellationToken);

            progress?.Report(new ImagingStudyLoadProgress(message: Resources.ImagingStudyService_FilesFoundProgress, processedFiles: allFiles.Count, totalFiles: allFiles.Count));

            var dicomCandidateFiles = allFiles.Where(IsDicomCandidate).ToList();

            var imageFiles = allFiles.Where(IsImageCandidate).ToList();

            progress?.Report(new ImagingStudyLoadProgress(message: Resources.ImagingStudyService_DicomMetadataReadProgress, processedFiles: 0, totalFiles: allFiles.Count));

            var dicomImportResult = await _base!.CoreBase!.Dicom!.ImportService!.ImportFolderAsync(folderPath, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var studyId = !string.IsNullOrWhiteSpace(dicomImportResult?.StudyInstanceUid) ? dicomImportResult.StudyInstanceUid! : $"folder-study-{Guid.NewGuid():N}";

            var studyName = !string.IsNullOrWhiteSpace(dicomImportResult?.StudyDescription) ? dicomImportResult.StudyDescription! : folderName;

            var series = new List<SeriesInfo>();
            var seriesFiles = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            var seriesDicomMetadata = new Dictionary<string, IReadOnlyList<DicomFileMetadata>>(StringComparer.OrdinalIgnoreCase);

            var seriesNumber = 1;

            var validDicomFileCount = 0;

            if (dicomImportResult != null)
            {
                foreach (var importedSeries in dicomImportResult.Series)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (importedSeries.Files.Count == 0)
                    {
                        continue;
                    }

                    validDicomFileCount += importedSeries.Files.Count;

                    var seriesId = CreateSeriesId(studyId, importedSeries.SeriesInstanceUid, seriesNumber);

                    var modality = string.IsNullOrWhiteSpace(importedSeries.Modality) ? Resources.ImagingStudyService_Dicom : importedSeries.Modality!;

                    var name = string.IsNullOrWhiteSpace(importedSeries.DisplayName) ? $"{Resources.ImagingStudyService_Dicom}-{Resources.ImagingStudyService_Series} {seriesNumber}" : importedSeries.DisplayName;

                    var description = string.IsNullOrWhiteSpace(importedSeries.SeriesInstanceUid) ? $"{importedSeries.InstanceCount} {Resources.ImagingStudyService_Dicom}-{Resources.ImagingStudyService_Files}" : $"{importedSeries.InstanceCount} {Resources.ImagingStudyService_Dicom}-{Resources.ImagingStudyService_Files}";

                    series.Add(new SeriesInfo(id: seriesId, modality: modality, name: name, description: description, imageCount: importedSeries.InstanceCount, studyId: studyId, seriesNumber: seriesNumber));

                    seriesFiles[seriesId] = [.. importedSeries.Files.Select(item => item.FilePath)];
                    seriesDicomMetadata[seriesId] = [.. importedSeries.Files];

                    seriesNumber++;
                }
            }

            if (imageFiles.Count > 0)
            {
                var seriesId = $"{studyId}-images";

                series.Add(new SeriesInfo(id: seriesId, modality: "IMG", name: Resources.ImagingStudyService_ImageFiles, description: $"{imageFiles.Count} {Resources.ImagingStudyService_ImageFiles} {Resources.ImagingStudyService_Found}", imageCount: imageFiles.Count, studyId: studyId, seriesNumber: seriesNumber++));

                seriesFiles[seriesId] = [.. imageFiles];
            }

            if (series.Count == 0 && allFiles.Count > 0)
            {
                var seriesId = $"{studyId}-files";

                series.Add(new SeriesInfo(id: seriesId, modality: "FILES", name: Resources.ImagingStudyService_FolderContent, description: $"{allFiles.Count} {Resources.ImagingStudyService_Files} {Resources.ImagingStudyService_InSelectedFolder}", imageCount: allFiles.Count, studyId: studyId, seriesNumber: seriesNumber));

                seriesFiles[seriesId] = [.. allFiles];
            }

            var dicomFileSet = new HashSet<string>(seriesFiles.Where(item => item.Key.IndexOf("images", StringComparison.OrdinalIgnoreCase) < 0).SelectMany(item => item.Value), StringComparer.OrdinalIgnoreCase);

            var imageFileSet = new HashSet<string>(imageFiles, StringComparer.OrdinalIgnoreCase);

            var otherFilesCount = allFiles.Count(file => !dicomFileSet.Contains(file) && !imageFileSet.Contains(file));

            var summary = new ImagingFolderScanSummary(folderPath: folderPath, totalFiles: allFiles.Count, dicomCandidates: dicomCandidateFiles.Count, validDicomFiles: validDicomFileCount, imageFiles: imageFiles.Count, otherFiles: otherFilesCount);

            progress?.Report(new ImagingStudyLoadProgress(message: Resources.ImagingStudyService_ScanCompletedProgress, processedFiles: allFiles.Count, totalFiles: allFiles.Count));

            return new FolderScanResult(studyId, studyName, folderPath, series, seriesFiles, seriesDicomMetadata, summary);
        }

        private sealed class FolderScanResult(string studyId, string folderName, string folderPath, IReadOnlyList<SeriesInfo> series, IReadOnlyDictionary<string, IReadOnlyList<string>> seriesFiles, IReadOnlyDictionary<string, IReadOnlyList<DicomFileMetadata>> seriesDicomMetadata, ImagingFolderScanSummary summary)
        {
            public string FolderName { get; } = folderName;

            public string FolderPath { get; } = folderPath;

            public IReadOnlyList<SeriesInfo> Series { get; } = series;

            public IReadOnlyDictionary<string, IReadOnlyList<DicomFileMetadata>> SeriesDicomMetadata { get; } = seriesDicomMetadata;
            public IReadOnlyDictionary<string, IReadOnlyList<string>> SeriesFiles { get; } = seriesFiles;
            public string StudyId { get; } = studyId;

            public ImagingFolderScanSummary Summary { get; } = summary;
        }
    }
}