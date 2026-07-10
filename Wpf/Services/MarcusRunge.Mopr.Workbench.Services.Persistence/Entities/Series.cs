using MarcusRunge.Base;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Entities
{
    /// <summary>
    /// Represents a DICOM series.
    /// </summary>
    public class Series : BindableEntityBase
    {
        private string? _seriesInstanceUid, _description, _modality;
        private Study? _study;
        private int _studyId;

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string? Description { get => _description; set => SetProperty(ref _description, value); }

        /// <summary>
        /// Gets or sets the instances.
        /// </summary>
        public ICollection<Instance> Instances { get; set; } = new HashSet<Instance>();

        /// <summary>
        /// Gets or sets the modality.
        /// </summary>
        public string? Modality { get => _modality; set => SetProperty(ref _modality, value); }

        /// <summary>
        /// Gets or sets the series instance UID.
        /// </summary>
        public string? SeriesInstanceUid { get => _seriesInstanceUid; set => SetProperty(ref _seriesInstanceUid, value); }

        /// <summary>
        /// Gets or sets the study.
        /// </summary>
        public Study? Study { get => _study; set => SetProperty(ref _study, value); }

        /// <summary>
        /// Gets or sets the study ID.
        /// </summary>
        public int StudyId { get => _studyId; set => SetProperty(ref _studyId, value); }
    }
}