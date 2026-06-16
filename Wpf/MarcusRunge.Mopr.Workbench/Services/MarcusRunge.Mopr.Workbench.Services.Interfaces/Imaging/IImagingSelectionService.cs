using MarcusRunge.Mopr.Workbench.Contracts.Models;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging
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