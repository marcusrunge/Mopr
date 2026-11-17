using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements;
using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Imaging
{
    /// <summary>
    /// Represents the persistable business state of one imaging measurement.
    /// </summary>
    public sealed record ImagingMeasurementChangeData
    {
        /// <summary>
        /// Initializes one validated imaging measurement state.
        /// </summary>
        public ImagingMeasurementChangeData(MeasurementType measurementType, MeasurementData data, string? title = null, string? description = null)
        {
            if (measurementType == MeasurementType.Unknown) throw new ArgumentException("A persistable measurement requires a supported measurement type.", nameof(measurementType));

            MeasurementType = measurementType;
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Title = title;
            Description = description;
        }

        public MeasurementType MeasurementType { get; }
        public MeasurementData Data { get; }
        public string? Title { get; }
        public string? Description { get; }
    }
}
