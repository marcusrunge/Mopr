using MarcusRunge.Mopr.Workbench.Contracts.Models;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public sealed class SeriesSelectionChangedEventArgs(StudyInfo? selectedStudy, SeriesInfo? selectedSeries) : EventArgs
    {
        public SeriesInfo? SelectedSeries { get; } = selectedSeries;
        public StudyInfo? SelectedStudy { get; } = selectedStudy;
    }
}