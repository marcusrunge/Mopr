using System.Collections.Generic;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts
{
    public sealed class DicomImportResult
    {
        public DicomImportResult(string folderPath, string? studyInstanceUid, string? studyDescription, IReadOnlyList<DicomSeriesImportResult> series)
        {
            FolderPath = folderPath;
            StudyInstanceUid = studyInstanceUid;
            StudyDescription = studyDescription;
            Series = series;
        }

        public string FolderPath { get; }

        public IReadOnlyList<DicomSeriesImportResult> Series { get; }
        public int SeriesCount => Series.Count;
        public string? StudyDescription { get; }
        public string? StudyInstanceUid { get; }
    }
}