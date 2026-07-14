using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public interface IImagingStudyService
    {
        event EventHandler<ImagingStudyLoadedEventArgs>? StudyLoaded;

        IReadOnlyList<SeriesInfo> CurrentSeries { get; }
        StudyInfo? CurrentStudy { get; }
        ImagingFolderScanSummary? LastScanSummary { get; }

        void Clear();

        IReadOnlyList<DicomFileMetadata> GetDicomMetadataForSeries(string seriesId);

        IReadOnlyList<string> GetFilesForSeries(string seriesId);

        DicomFileMetadata? GetFirstDicomMetadataForSeries(string seriesId);

        Task LoadStudyFromFolderAsync(string folderPath, IProgress<ImagingStudyLoadProgress>? progress = null, CancellationToken cancellationToken = default);
    }
}