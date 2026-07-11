using MarcusRunge.Base;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Entities
{
    /// <summary>
    /// Represents a DICOM study.
    /// </summary>
    public class Study : AuditableEntityBase
    {       
        private string? _studyInstanceUid, _accessionNumber, _description;

        /// <summary>
        /// Gets or sets the accession number.
        /// </summary>
        public string? AccessionNumber { get => _accessionNumber; set => SetProperty(ref _accessionNumber, value); }
                
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string? Description { get => _description; set => SetProperty(ref _description, value); }
                
        /// <summary>
        /// Gets or sets the series.
        /// </summary>
        public ICollection<Series> Series { get; set; } = new HashSet<Series>();

        /// <summary>
        /// Gets or sets the study instance UID.
        /// </summary>
        public string? StudyInstanceUid { get => _studyInstanceUid; set => SetProperty(ref _studyInstanceUid, value); }
    }
}