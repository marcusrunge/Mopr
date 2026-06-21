using MarcusRunge.Mopr.Workbench.Contracts.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public interface IImagingSelectionService
    {
        event EventHandler<SeriesSelectionChangedEventArgs>? SelectedSeriesChanged;

        SeriesInfo? SelectedSeries { get; }
        StudyInfo? SelectedStudy { get; }

        void ClearSelection();

        void SelectSeries(SeriesInfo? series);

        void SelectStudy(StudyInfo? study);
    }
}