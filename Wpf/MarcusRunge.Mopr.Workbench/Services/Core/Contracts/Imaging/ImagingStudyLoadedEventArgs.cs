using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using System;
using System.Collections.Generic;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public sealed class ImagingStudyLoadedEventArgs : EventArgs
    {
        public ImagingStudyLoadedEventArgs(StudyInfo? study, IReadOnlyList<SeriesInfo> series, ImagingFolderScanSummary? scanSummary = null)
        {
            Study = study;
            Series = series;
            ScanSummary = scanSummary;
        }

        public ImagingFolderScanSummary? ScanSummary { get; }
        public IReadOnlyList<SeriesInfo> Series { get; }
        public StudyInfo? Study { get; }
    }
}