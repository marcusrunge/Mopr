using MarcusRunge.Mopr.Workbench.Contracts.Models;
using System;
using System.Collections.Generic;

namespace MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging
{
    public interface IImagingStudyService
    {
        event EventHandler<ImagingStudyLoadedEventArgs>? StudyLoaded;

        IReadOnlyList<SeriesInfo> CurrentSeries { get; }
        StudyInfo? CurrentStudy { get; }

        void Clear();

        void LoadDemoStudy();
    }
}