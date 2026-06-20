using MarcusRunge.Mopr.Workbench.Contracts.Models;
using MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging;
using System;
using System.Collections.Generic;

namespace MarcusRunge.Mopr.Workbench.Services.Imaging
{
    public sealed class ImagingStudyService : IImagingStudyService
    {
        private readonly List<SeriesInfo> _currentSeries = new List<SeriesInfo>();

        private StudyInfo? _currentStudy;

        public event EventHandler<ImagingStudyLoadedEventArgs>? StudyLoaded;

        public IReadOnlyList<SeriesInfo> CurrentSeries => _currentSeries;
        public StudyInfo? CurrentStudy => _currentStudy;

        public void Clear()
        {
            _currentStudy = null;
            _currentSeries.Clear();

            RaiseStudyLoaded();
        }

        public void LoadDemoStudy()
        {
            var study = new StudyInfo(id: "demo-study", name: "MRI Brain", description: "Demo Studie");

            var series = new List<SeriesInfo>
            {
                new SeriesInfo(id: "mr-t1-axial", modality: "MR", name: "T1 axial", description: "T1 gewichtete axiale Serie", imageCount: 128, studyId: "demo-study", seriesNumber: 1),
                new SeriesInfo(id: "mr-t2-axial", modality: "MR", name: "T2 axial", description: "T2 gewichtete axiale Serie", imageCount: 128, studyId: "demo-study", seriesNumber: 2),
                new SeriesInfo(id: "mr-flair-coronal", modality: "MR", name: "FLAIR coronal", description: "FLAIR koronale Serie", imageCount: 96, studyId: "demo-study", seriesNumber: 3),
                new SeriesInfo(id: "ct-axial", modality: "CT", name: "CT axial", description: "CT axiale Rekonstruktion", imageCount: 320, studyId: "demo-study", seriesNumber: 4),
                new SeriesInfo(id: "mpr", modality: "MPR", name: "MPR", description: "Multiplanare Rekonstruktion", imageCount: 1, studyId: "demo-study", seriesNumber: 5)
            };

            _currentStudy = study;

            _currentSeries.Clear();
            _currentSeries.AddRange(series);

            RaiseStudyLoaded();
        }

        private void RaiseStudyLoaded() => StudyLoaded?.Invoke(this, new ImagingStudyLoadedEventArgs(_currentStudy, _currentSeries.ToArray()));
    }
}