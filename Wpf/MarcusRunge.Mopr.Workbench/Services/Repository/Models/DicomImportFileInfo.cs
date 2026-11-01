namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Contains validated identifiers and normalized descriptive metadata for one DICOM import candidate.
    /// </summary>
    public sealed class DicomImportFileInfo
    {
        public string? AccessionNumber { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int? InstanceNumber { get; set; }
        public bool IsDicomFile { get; set; }
        public bool IsImportable => IsDicomFile && !string.IsNullOrWhiteSpace(StudyInstanceUid) && !string.IsNullOrWhiteSpace(SeriesInstanceUid) && !string.IsNullOrWhiteSpace(SopInstanceUid);
        public string? Modality { get; set; }
        public DateTime? PatientBirthDate { get; set; }
        public string? PatientId { get; set; }
        public string? PatientName { get; set; }
        public string RelativeRepositoryPath { get; set; } = string.Empty;
        public string? SeriesDescription { get; set; }
        public string SeriesInstanceUid { get; set; } = string.Empty;
        public int? SeriesNumber { get; set; }
        public string SopInstanceUid { get; set; } = string.Empty;
        public DateTime? StudyDate { get; set; }
        public string? StudyDescription { get; set; }
        public string StudyInstanceUid { get; set; } = string.Empty;
    }
}
