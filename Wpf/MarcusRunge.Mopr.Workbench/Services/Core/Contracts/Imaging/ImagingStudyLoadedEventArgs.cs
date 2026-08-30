using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using System;
using System.Collections.Generic;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public sealed class ImagingStudyLoadedEventArgs(StudyInfo? study, IReadOnlyList<SeriesInfo> series, ImagingFolderScanSummary? scanSummary = null) : EventArgs
    {
        public ImagingFolderScanSummary? ScanSummary { get; } = scanSummary;
        public IReadOnlyList<SeriesInfo> Series { get; } = series;
        public StudyInfo? Study { get; } = study;
    }
}