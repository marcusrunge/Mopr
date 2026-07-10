using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements;
using System.Text.Json;

namespace MarcusRunge.Mopr.Workbench.Services.Persistence.Serialization
{
    internal static class MeasurementDataSerializer
    {
        internal static MeasurementData? Deserialize(MeasurementType measurementType, string json) => measurementType switch
        {
            MeasurementType.Angle => JsonSerializer.Deserialize<AngleMeasurementData>(json),
            MeasurementType.Annotation => JsonSerializer.Deserialize<AnnotationMeasurementData>(json),
            MeasurementType.Area => JsonSerializer.Deserialize<AreaMeasurementData>(json),
            MeasurementType.Ellipse => JsonSerializer.Deserialize<EllipseMeasurementData>(json),
            MeasurementType.Freehand => JsonSerializer.Deserialize<FreehandMeasurementData>(json),
            MeasurementType.Length => JsonSerializer.Deserialize<LengthMeasurementData>(json),
            MeasurementType.Point => JsonSerializer.Deserialize<PointMeasurementData>(json),
            MeasurementType.Polygon => JsonSerializer.Deserialize<PolygonMeasurementData>(json),
            MeasurementType.Rectangle => JsonSerializer.Deserialize<RectangleMeasurementData>(json),
            MeasurementType.Roi3D => JsonSerializer.Deserialize<Roi3DMeasurementData>(json),
            MeasurementType.Segmentation => JsonSerializer.Deserialize<SegmentationMeasurementData>(json),
            MeasurementType.Volume => JsonSerializer.Deserialize<VolumeMeasurementData>(json),
            MeasurementType.Unknown => null,
            _ => throw new NotSupportedException($"Unsupported measurement type '{measurementType}'.")
        };

        internal static MeasurementType GetMeasurementType(MeasurementData measurementData) => measurementData switch
        {
            AngleMeasurementData => MeasurementType.Angle,
            AnnotationMeasurementData => MeasurementType.Annotation,
            AreaMeasurementData => MeasurementType.Area,
            EllipseMeasurementData => MeasurementType.Ellipse,
            FreehandMeasurementData => MeasurementType.Freehand,
            LengthMeasurementData => MeasurementType.Length,
            PointMeasurementData => MeasurementType.Point,
            PolygonMeasurementData => MeasurementType.Polygon,
            RectangleMeasurementData => MeasurementType.Rectangle,
            Roi3DMeasurementData => MeasurementType.Roi3D,
            SegmentationMeasurementData => MeasurementType.Segmentation,
            VolumeMeasurementData => MeasurementType.Volume,
            _ => MeasurementType.Unknown
        };

        internal static string Serialize(MeasurementData measurementData)
        {
            ArgumentNullException.ThrowIfNull(measurementData);
            return JsonSerializer.Serialize(measurementData, measurementData.GetType());
        }
    }
}