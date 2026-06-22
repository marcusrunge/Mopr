using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
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
        private readonly List<SeriesInfo> _currentSeries = new List<SeriesInfo>();

        private StudyInfo? _currentStudy;

        public event EventHandler<ImagingStudyLoadedEventArgs>? StudyLoaded;

        public IReadOnlyList<SeriesInfo> CurrentSeries => _currentSeries;
        public StudyInfo? CurrentStudy => _currentStudy;

        public void Clear()
        {
            _currentStudy = null;
            _currentSeries.Clear();

            RaiseStudyLoaded();
        }

        public void LoadDemoStudy()
        {
            var study = new StudyInfo(id: "demo-study", name: "MRI Brain", description: "Demo Studie");

            var series = new List<SeriesInfo>
            {
                new SeriesInfo(id: "mr-t1-axial", modality: "MR", name: "T1 axial", description: "T1 gewichtete axiale Serie", imageCount: 128, studyId: "demo-study", seriesNumber: 1),
                new SeriesInfo(id: "mr-t2-axial", modality: "MR", name: "T2 axial", description: "T2 gewichtete axiale Serie", imageCount: 128, studyId: "demo-study", seriesNumber: 2),
                new SeriesInfo(id: "mr-flair-coronal", modality: "MR", name: "FLAIR coronal", description: "FLAIR koronale Serie", imageCount: 96, studyId: "demo-study", seriesNumber: 3),
                new SeriesInfo(id: "ct-axial", modality: "CT", name: "CT axial", description: "CT axiale Rekonstruktion", imageCount: 320, studyId: "demo-study", seriesNumber: 4),
                new SeriesInfo(id: "mpr", modality: "MPR", name: "MPR", description: "Multiplanare Rekonstruktion", imageCount: 1, studyId: "demo-study", seriesNumber: 5)
            };

            _currentStudy = study;

            _currentSeries.Clear();
            _currentSeries.AddRange(series);

            RaiseStudyLoaded();
        }

        public async Task LoadStudyFromFolderAsync(string folderPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var scanResult = await Task.Run(() => ScanFolder(folderPath, cancellationToken), cancellationToken);

            var study = new StudyInfo(id: scanResult.StudyId, name: scanResult.FolderName, description: scanResult.FolderPath);

            ApplyStudy(study, scanResult.Series);
        }

        protected override void OnCreate(IImagingServiceBase @base)
        {
        }

        protected override Task OnCreateAsync(IImagingServiceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;

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

        private static bool IsDicomFile(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                if (!fileInfo.Exists || fileInfo.Length < 132)
                {
                    return false;
                }

                using var stream = File.OpenRead(filePath);

                var buffer = new byte[132];
                var read = stream.Read(buffer, 0, buffer.Length);

                if (read < 132)
                {
                    return false;
                }

                return buffer[128] == (byte)'D' && buffer[129] == (byte)'I' && buffer[130] == (byte)'C' && buffer[131] == (byte)'M';
            }
            catch
            {
                return false;
            }
        }

        private static bool IsImageCandidate(string filePath)
        {
            var extension = Path.GetExtension(filePath);

            return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".tif", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".tiff", StringComparison.OrdinalIgnoreCase);
        }

        private static FolderScanResult ScanFolder(string folderPath, CancellationToken cancellationToken)
        {
            var folderName = GetSafeFolderName(folderPath);
            var studyId = "folder-study-" + Guid.NewGuid().ToString("N");

            var allFiles = new List<string>();

            foreach (var file in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                allFiles.Add(file);
            }

            var dicomCandidateCount = allFiles.Count(IsDicomCandidate);

            var dicomFiles = allFiles.Where(IsDicomFile).ToList();

            var imageFiles = allFiles.Where(IsImageCandidate).ToList();

            var series = new List<SeriesInfo>();
            var seriesNumber = 1;

            if (dicomFiles.Count > 0)
            {
                series.Add(new SeriesInfo(id: studyId + "-dicom", modality: "DICOM", name: "DICOM-Dateien", description: $"{dicomFiles.Count} valide DICOM-Dateien, {dicomCandidateCount} Kandidaten", imageCount: dicomFiles.Count, studyId: studyId, seriesNumber: seriesNumber++));
            }

            if (dicomFiles.Count == 0 && dicomCandidateCount > 0)
            {
                series.Add(new SeriesInfo(id: studyId + "-dicom-candidates", modality: "DICOM?", name: "DICOM-Kandidaten", description: $"{dicomCandidateCount} Kandidaten gefunden, aber kein DICM-Marker erkannt", imageCount: dicomCandidateCount, studyId: studyId, seriesNumber: seriesNumber++));
            }

            if (imageFiles.Count > 0)
            {
                series.Add(new SeriesInfo(id: studyId + "-images", modality: "IMG", name: "Bilddateien", description: $"{imageFiles.Count} Bilddateien gefunden", imageCount: imageFiles.Count, studyId: studyId, seriesNumber: seriesNumber++));
            }

            if (series.Count == 0 && allFiles.Count > 0)
            {
                series.Add(new SeriesInfo(id: studyId + "-files", modality: "FILES", name: "Ordnerinhalt", description: $"{allFiles.Count} Dateien im ausgewählten Ordner", imageCount: allFiles.Count, studyId: studyId, seriesNumber: seriesNumber));
            }

            return new FolderScanResult(studyId, folderName, folderPath, series);
        }

        private void ApplyStudy(StudyInfo study, IReadOnlyList<SeriesInfo> series)
        {
            _currentStudy = study;

            _currentSeries.Clear();
            _currentSeries.AddRange(series);

            RaiseStudyLoaded();
        }

        private void RaiseStudyLoaded() => StudyLoaded?.Invoke(this, new ImagingStudyLoadedEventArgs(_currentStudy, _currentSeries.ToArray()));

        private sealed class FolderScanResult
        {
            public FolderScanResult(string studyId, string folderName, string folderPath, IReadOnlyList<SeriesInfo> series)
            {
                StudyId = studyId;
                FolderName = folderName;
                FolderPath = folderPath;
                Series = series;
            }

            public string FolderName { get; }
            public string FolderPath { get; }
            public IReadOnlyList<SeriesInfo> Series { get; }
            public string StudyId { get; }
        }
    }
}