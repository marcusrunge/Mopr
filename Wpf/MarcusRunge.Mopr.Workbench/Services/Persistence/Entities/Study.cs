using MarcusRunge.Base;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Entities
{
    /// <summary>
    /// Represents a DICOM study.
    /// </summary>
    public class Study : AuditableEntityBase
    {       
        private DateTime? _patientBirthDate, _studyDate;
        private string? _studyInstanceUid, _accessionNumber, _description, _patientId, _patientName;

        /// <summary>
        /// Gets or sets the accession number.
        /// </summary>
        public string? AccessionNumber { get => _accessionNumber; set => SetProperty(ref _accessionNumber, value); }
                
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string? Description { get => _description; set => SetProperty(ref _description, value); }
                

        /// <summary>
        /// Gets or sets the patient birth date supplied by the DICOM study.
        /// </summary>
        public DateTime? PatientBirthDate { get => _patientBirthDate; set => SetProperty(ref _patientBirthDate, value?.Date); }

        /// <summary>
        /// Gets or sets the patient identifier supplied by the DICOM study.
        /// </summary>
        public string? PatientId { get => _patientId; set => SetProperty(ref _patientId, value); }

        /// <summary>
        /// Gets or sets the normalized patient display name supplied by the DICOM study.
        /// </summary>
        public string? PatientName { get => _patientName; set => SetProperty(ref _patientName, value); }

        /// <summary>
        /// Gets or sets the series.
        /// </summary>
        public ICollection<Series> Series { get; set; } = new HashSet<Series>();


        /// <summary>
        /// Gets or sets the study date and time supplied by DICOM.
        /// </summary>
        public DateTime? StudyDate { get => _studyDate; set => SetProperty(ref _studyDate, value); }

        /// <summary>
        /// Gets or sets the study instance UID.
        /// </summary>
        public string? StudyInstanceUid { get => _studyInstanceUid; set => SetProperty(ref _studyInstanceUid, value); }
    }
}
