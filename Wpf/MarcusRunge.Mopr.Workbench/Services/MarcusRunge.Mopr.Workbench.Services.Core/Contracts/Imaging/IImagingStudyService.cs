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

        void Clear();

        void LoadDemoStudy();

        Task LoadStudyFromFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    }
}