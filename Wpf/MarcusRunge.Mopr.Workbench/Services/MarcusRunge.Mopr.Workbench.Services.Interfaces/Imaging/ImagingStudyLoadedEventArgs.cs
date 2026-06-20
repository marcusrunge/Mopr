using MarcusRunge.Mopr.Workbench.Contracts.Models;
using System;
using System.Collections.Generic;

namespace MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging
{
    public sealed class ImagingStudyLoadedEventArgs : EventArgs
    {
        public ImagingStudyLoadedEventArgs(StudyInfo? study, IReadOnlyList<SeriesInfo> series)
        {
            Study = study;
            Series = series;
        }

        public IReadOnlyList<SeriesInfo> Series { get; }
        public StudyInfo? Study { get; }
    }
}