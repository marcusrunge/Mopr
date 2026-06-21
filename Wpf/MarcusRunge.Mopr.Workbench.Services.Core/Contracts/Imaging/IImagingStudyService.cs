using MarcusRunge.Mopr.Workbench.Contracts.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
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