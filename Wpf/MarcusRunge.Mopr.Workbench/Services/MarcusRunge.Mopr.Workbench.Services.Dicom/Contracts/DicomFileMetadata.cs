namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts
{
    public sealed class DicomFileMetadata
    {
        public DicomFileMetadata(string filePath, string? studyInstanceUid, string? seriesInstanceUid, string? sopInstanceUid, string? modality, string? studyDescription, string? seriesDescription, int? instanceNumber, int? rows, int? columns)
        {
            FilePath = filePath;
            StudyInstanceUid = studyInstanceUid;
            SeriesInstanceUid = seriesInstanceUid;
            SopInstanceUid = sopInstanceUid;
            Modality = modality;
            StudyDescription = studyDescription;
            SeriesDescription = seriesDescription;
            InstanceNumber = instanceNumber;
            Rows = rows;
            Columns = columns;
        }

        public int? Columns { get; }

        public string DisplayTitle => string.IsNullOrWhiteSpace(SeriesDescription) ? Modality ?? "DICOM-Serie" : SeriesDescription;

        public string FilePath { get; }

        public int? InstanceNumber { get; }
        public string? Modality { get; }
        public int? Rows { get; }
        public string? SeriesDescription { get; }
        public string? SeriesInstanceUid { get; }
        public string? SopInstanceUid { get; }
        public string? StudyDescription { get; }
        public string? StudyInstanceUid { get; }
    }
}