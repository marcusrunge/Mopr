using MarcusRunge.Mopr.Workbench.Contracts.Models;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public sealed class SeriesSelectionChangedEventArgs : EventArgs
    {
        public SeriesSelectionChangedEventArgs(StudyInfo? selectedStudy, SeriesInfo? selectedSeries)
        {
            SelectedStudy = selectedStudy;
            SelectedSeries = selectedSeries;
        }

        public SeriesInfo? SelectedSeries { get; }
        public StudyInfo? SelectedStudy { get; }
    }
}