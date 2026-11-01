using System;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Models
{
    /// <summary>
    /// Contains normalized metadata read from one DICOM file.
    /// </summary>
    public sealed class DicomFileMetadata
    {
        public DicomFileMetadata(string filePath, string? studyInstanceUid, string? seriesInstanceUid, string? sopInstanceUid, string? modality, string? studyDescription, string? seriesDescription, int? instanceNumber, int? rows, int? columns, string? patientName = null, DateTime? patientBirthDate = null, string? patientId = null, DateTime? studyDate = null, string? accessionNumber = null, int? seriesNumber = null)
        {
            AccessionNumber = NormalizeOptionalValue(accessionNumber);
            Columns = columns;
            FilePath = filePath;
            InstanceNumber = instanceNumber;
            Modality = NormalizeOptionalValue(modality);
            PatientBirthDate = patientBirthDate?.Date;
            PatientId = NormalizeOptionalValue(patientId);
            PatientName = NormalizeOptionalValue(patientName);
            Rows = rows;
            SeriesDescription = NormalizeOptionalValue(seriesDescription);
            SeriesInstanceUid = NormalizeOptionalValue(seriesInstanceUid);
            SeriesNumber = seriesNumber;
            SopInstanceUid = NormalizeOptionalValue(sopInstanceUid);
            StudyDate = studyDate;
            StudyDescription = NormalizeOptionalValue(studyDescription);
            StudyInstanceUid = NormalizeOptionalValue(studyInstanceUid);
        }

        public string? AccessionNumber { get; }
        public int? Columns { get; }
        public string DisplayTitle => string.IsNullOrWhiteSpace(SeriesDescription) ? Modality ?? "DICOM-Serie" : SeriesDescription;
        public string FilePath { get; }
        public int? InstanceNumber { get; }
        public string? Modality { get; }
        public DateTime? PatientBirthDate { get; }
        public string? PatientId { get; }
        public string? PatientName { get; }
        public int? Rows { get; }
        public string? SeriesDescription { get; }
        public string? SeriesInstanceUid { get; }
        public int? SeriesNumber { get; }
        public string? SopInstanceUid { get; }
        public DateTime? StudyDate { get; }
        public string? StudyDescription { get; }
        public string? StudyInstanceUid { get; }

        private static string? NormalizeOptionalValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
