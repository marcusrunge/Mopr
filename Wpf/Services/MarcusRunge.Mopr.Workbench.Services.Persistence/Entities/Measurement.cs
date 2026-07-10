using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Entities
{
    /// <summary>
    /// Represents a persisted measurement.
    /// </summary>
    public class Measurement : AuditableEntityBase
    {
        private Instance? _instance;
        private int _instanceId;
        private MeasurementType _measurementType;
        private string? _title, _dataJson, _description;
                
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
        /// Gets or sets the title of the measurement.
        /// </summary>
        public string? Title { get => _title; set => SetProperty(ref _title, value); }
    }
}