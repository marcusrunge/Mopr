using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Persistence.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Entities
{
    /// <summary>
    /// Represents a persisted measurement.
    /// </summary>
    public class Measurement : BindableEntityBase
    {
        private DateTime _createdAtUtc = DateTime.UtcNow;
        private User? _createdByUser, _modifiedByUser;
        private Instance? _instance;
        private int _instanceId, _createdByUserId, _modifiedByUserId;
        private MeasurementType _measurementType;
        private DateTime? _modifiedAtUtc;
        private string? _title, _dataJson, _description;

        /// <summary>
        /// Gets or sets the UTC date and time when the measurement was created.
        /// </summary>
        public DateTime CreatedAtUtc { get => _createdAtUtc; set => SetProperty(ref _createdAtUtc, value); }

        /// <summary>
        /// Gets or sets the user who created the measurement.
        /// </summary>
        public User? CreatedByUser { get => _createdByUser; set => SetProperty(ref _createdByUser, value); }

        /// <summary>
        /// Gets or sets the ID of the user who created the measurement.
        /// </summary>
        public int CreatedByUserId { get => _createdByUserId; set => SetProperty(ref _createdByUserId, value); }

        /// <summary>
        /// Gets or sets the serialized measurement data.
        /// </summary>
        public string? DataJson { get => _dataJson; set => SetProperty(ref _dataJson, value); }

        /// <summary>
        /// Gets or sets the description of the measurement.
        /// </summary>
        public string? Description { get => _description; set => SetProperty(ref _description, value); }
               
        /// <summary>
        /// Gets or sets the instance to which the measurement belongs.
        /// </summary>
        public Instance? Instance { get => _instance; set => SetProperty(ref _instance, value); }

        /// <summary>
        /// Gets or sets the ID of the instance to which the measurement belongs.
        /// </summary>
        public int InstanceId { get => _instanceId; set => SetProperty(ref _instanceId, value); }

        /// <summary>
        /// Gets or sets the measurement type.
        /// </summary>
        public MeasurementType MeasurementType { get => _measurementType; set => SetProperty(ref _measurementType, value); }

        /// <summary>
        /// Gets or sets the UTC date and time when the measurement was last modified.
        /// </summary>
        public DateTime? ModifiedAtUtc { get => _modifiedAtUtc; set => SetProperty(ref _modifiedAtUtc, value); }

        /// <summary>
        /// Gets or sets the user who last modified the measurement.
        /// </summary>
        public User? ModifiedByUser { get => _modifiedByUser; set => SetProperty(ref _modifiedByUser, value); }

        /// <summary>
        /// Gets or sets the ID of the user who last modified the measurement.
        /// </summary>
        public int ModifiedByUserId { get => _modifiedByUserId; set => SetProperty(ref _modifiedByUserId, value); }

        /// <summary>
        /// Gets or sets the title of the measurement.
        /// </summary>
        public string? Title { get => _title; set => SetProperty(ref _title, value); }
    }
}