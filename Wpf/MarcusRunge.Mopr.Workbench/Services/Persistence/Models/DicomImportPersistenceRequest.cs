namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Models
{
    /// <summary>
    /// Contains the validated medical and repository relationships required to persist one imported DICOM instance atomically.
    /// </summary>
    public sealed class DicomImportPersistenceRequest
    {
        public string? AccessionNumber { get; set; }
        public int CreatedByUserId { get; set; }
        public int? InstanceNumber { get; set; }
        public string? Modality { get; set; }
        public DateTime? PatientBirthDate { get; set; }
        public string? PatientId { get; set; }
        public string? PatientName { get; set; }
        public string RelativeFilePath { get; set; } = string.Empty;
        public int RepositoryLocationId { get; set; }
        public string? SeriesDescription { get; set; }
        public string SeriesInstanceUid { get; set; } = string.Empty;
        public int? SeriesNumber { get; set; }
        public string SopInstanceUid { get; set; } = string.Empty;
        public DateTime? StudyDate { get; set; }
        public string? StudyDescription { get; set; }
        public string StudyInstanceUid { get; set; } = string.Empty;
    }
}
