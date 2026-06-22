using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
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

        void LoadDemoStudy();

        Task LoadStudyFromFolderAsync(string folderPath, IProgress<ImagingStudyLoadProgress>? progress = null, CancellationToken cancellationToken = default);
    }
}