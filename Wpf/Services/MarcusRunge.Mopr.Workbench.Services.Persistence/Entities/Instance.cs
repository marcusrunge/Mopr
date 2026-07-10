using MarcusRunge.Base;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Entities
{
    /// <summary>
    /// Represents a DICOM SOP instance.
    /// </summary>
    public class Instance : AuditableEntityBase
    {
        private Series? _series;
        private int _seriesId;
        private int? _instanceNumber;
        private string? _sopInstanceUid, _filePath;

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string? FilePath { get => _filePath; set => SetProperty(ref _filePath, value); }

        /// <summary>
        /// Gets or sets the instance number.
        /// </summary>
        public int? InstanceNumber { get => _instanceNumber; set => SetProperty(ref _instanceNumber, value); }

        /// <summary>
        /// Gets or sets the measurements.
        /// </summary>
        public ICollection<Measurement> Measurements { get; set; } = new HashSet<Measurement>();

        /// <summary>
        /// Gets or sets the series.
        /// </summary>
        public Series? Series { get => _series; set => SetProperty(ref _series, value); }

        /// <summary>
        /// Gets or sets the series ID.
        /// </summary>
        public int SeriesId { get => _seriesId; set => SetProperty(ref _seriesId, value); }

        /// <summary>
        /// Gets or sets the SOP instance UID.
        /// </summary>
        public string? SopInstanceUid { get => _sopInstanceUid; set => SetProperty(ref _sopInstanceUid, value); }

        /// <summary>
        /// Gets or sets the unreal objects.
        /// </summary>
        public ICollection<UnrealObject> UnrealObjects { get; set; } = new HashSet<UnrealObject>();
    }
}