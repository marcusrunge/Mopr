using MarcusRunge.Mopr.Workbench.Contracts.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
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