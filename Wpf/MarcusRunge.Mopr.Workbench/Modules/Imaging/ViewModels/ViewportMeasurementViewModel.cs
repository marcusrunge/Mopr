using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Geometry;
using MarcusRunge.Mopr.Workbench.Contracts.Models.Measurements;
using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ViewportMeasurementViewModel : ViewModelBase
    {
        private readonly DateTime _createdAt;
        private readonly Guid _id;
        private readonly double _startImageX, _startImageY;
        public ViewportMeasurementViewModel(double startImageX, double startImageY) : this(Guid.NewGuid(), startImageX, startImageY, DateTime.UtcNow, modifiedAtUtc: null)
        {
        }

        internal ViewportMeasurementViewModel(Guid id, double startImageX, double startImageY, DateTime createdAtUtc, DateTime? modifiedAtUtc)
        {
            if (id == Guid.Empty) throw new ArgumentException("The measurement ID must not be empty.", nameof(id));
            _id = id;
            _startImageX = startImageX;
            _startImageY = startImageY;
            _createdAt = NormalizeUtc(createdAtUtc);
            _modifiedAt = modifiedAtUtc.HasValue ? NormalizeUtc(modifiedAtUtc.Value) : null;
        }
        private string? _description, _title;
        private double? _endImageX, _endImageY, _pixelSpacingX, _pixelSpacingY, _previewEndImageX, _previewEndImageY;
        private bool _isSelected;
        private DateTime? _modifiedAt;

        public DateTime CreatedAt => _createdAt;

        public string? Description
        {
            get => _description;
            set
            {
                if (SetProperty(ref _description, value))
                {
                    Touch();
                }
            }
        }

        public string DisplayTitle
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Title))
                {
                    return Title;
                }

                if (DistanceMillimeters.HasValue)
                {
                    return string.Format(
                        Resources.Measurement_DefaultTitleMillimeterFormat,
                        DistanceMillimeters.Value);
                }

                if (DistancePixels.HasValue)
                {
                    return string.Format(
                        Resources.Measurement_DefaultTitlePixelFormat,
                        DistancePixels.Value);
                }

                return Resources.Measurement_DefaultTitle;
            }
        }

        public double? DistanceMillimeters
        {
            get
            {
                if (!IsComplete || !HasPixelSpacing)
                {
                    return null;
                }

                return CalculatePhysicalDistance(
                    StartImageX,
                    StartImageY,
                    EndImageX!.Value,
                    EndImageY!.Value,
                    PixelSpacingX!.Value,
                    PixelSpacingY!.Value);
            }
        }

        public double? DistancePixels
        {
            get
            {
                if (!IsComplete)
                {
                    return null;
                }

                return CalculateDistance(StartImageX, StartImageY, EndImageX!.Value, EndImageY!.Value);
            }
        }

        public double? EndImageX
        {
            get => _endImageX;
            private set
            {
                if (SetProperty(ref _endImageX, value))
                {
                    RaiseMeasurementPropertiesChanged();
                }
            }
        }

        public double? EndImageY
        {
            get => _endImageY;
            private set
            {
                if (SetProperty(ref _endImageY, value))
                {
                    RaiseMeasurementPropertiesChanged();
                }
            }
        }

        public bool HasPixelSpacing => PixelSpacingX.HasValue && PixelSpacingY.HasValue && PixelSpacingX.Value > 0 && PixelSpacingY.Value > 0;

        public bool HasPreviewEndPoint => PreviewEndImageX.HasValue && PreviewEndImageY.HasValue;

        public Guid Id => _id;

        public bool IsComplete => EndImageX.HasValue && EndImageY.HasValue;

        public bool IsDraft => !IsComplete;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string LabelText
        {
            get
            {
                if (DistanceMillimeters.HasValue)
                {
                    return string.Format(Resources.Measurement_LabelMillimeterFormat, DistanceMillimeters.Value);
                }

                if (DistancePixels.HasValue)
                {
                    return string.Format(Resources.Measurement_LabelPixelFormat, DistancePixels.Value);
                }

                return string.Empty;
            }
        }

        public DateTime? ModifiedAt
        {
            get => _modifiedAt;
            private set => SetProperty(ref _modifiedAt, value);
        }

        public double? PixelSpacingX
        {
            get => _pixelSpacingX;
            private set
            {
                if (SetProperty(ref _pixelSpacingX, value))
                {
                    RaiseMeasurementPropertiesChanged();
                    RaisePreviewPropertiesChanged();
                }
            }
        }

        public double? PixelSpacingY
        {
            get => _pixelSpacingY;
            private set
            {
                if (SetProperty(ref _pixelSpacingY, value))
                {
                    RaiseMeasurementPropertiesChanged();
                    RaisePreviewPropertiesChanged();
                }
            }
        }

        public double? PreviewDistanceMillimeters
        {
            get
            {
                if (!HasPreviewEndPoint || !HasPixelSpacing)
                {
                    return null;
                }

                return CalculatePhysicalDistance(
                    StartImageX,
                    StartImageY,
                    PreviewEndImageX!.Value,
                    PreviewEndImageY!.Value,
                    PixelSpacingX!.Value,
                    PixelSpacingY!.Value);
            }
        }

        public double? PreviewDistancePixels
        {
            get
            {
                if (!HasPreviewEndPoint)
                {
                    return null;
                }

                return CalculateDistance(StartImageX, StartImageY, PreviewEndImageX!.Value, PreviewEndImageY!.Value);
            }
        }

        public double? PreviewEndImageX
        {
            get => _previewEndImageX;
            private set
            {
                if (SetProperty(ref _previewEndImageX, value))
                {
                    RaisePreviewPropertiesChanged();
                }
            }
        }

        public double? PreviewEndImageY
        {
            get => _previewEndImageY;
            private set
            {
                if (SetProperty(ref _previewEndImageY, value))
                {
                    RaisePreviewPropertiesChanged();
                }
            }
        }

        public string PreviewLabelText
        {
            get
            {
                if (PreviewDistanceMillimeters.HasValue)
                {
                    return string.Format(Resources.Measurement_LabelMillimeterFormat, PreviewDistanceMillimeters.Value);
                }

                if (PreviewDistancePixels.HasValue)
                {
                    return string.Format(Resources.Measurement_LabelPixelFormat, PreviewDistancePixels.Value);
                }

                return string.Empty;
            }
        }

        public double StartImageX => _startImageX;

        public double StartImageY => _startImageY;

        public string? Title
        {
            get => _title;
            set
            {
                if (SetProperty(ref _title, value))
                {
                    Touch();
                    RaisePropertyChanged(nameof(DisplayTitle));
                }
            }
        }

        public void Complete(double imageX, double imageY)
        {
            EndImageX = imageX;
            EndImageY = imageY;

            PreviewEndImageX = null;
            PreviewEndImageY = null;

            RaiseMeasurementPropertiesChanged();
            RaisePreviewPropertiesChanged();
            Touch();
        }

        internal static ViewportMeasurementViewModel CreateLoaded(Guid id, double startImageX, double startImageY, double endImageX, double endImageY, double? pixelSpacingX, double? pixelSpacingY, string? title, string? description, DateTime createdAtUtc, DateTime? modifiedAtUtc)
        {
            var measurement = new ViewportMeasurementViewModel(id, startImageX, startImageY, createdAtUtc, modifiedAtUtc)
            {
                _endImageX = endImageX,
                _endImageY = endImageY,
                _pixelSpacingX = pixelSpacingX,
                _pixelSpacingY = pixelSpacingY,
                _title = title,
                _description = description
            };
            return measurement;
        }

        public ImagingMeasurementChangeData ToChangeData()
        {
            if (!IsComplete) throw new InvalidOperationException($"Measurement '{Id}' is not complete.");
            var data = new LengthMeasurementData { StartPoint = new Point2D { X = StartImageX, Y = StartImageY }, EndPoint = new Point2D { X = EndImageX!.Value, Y = EndImageY!.Value } };
            return new ImagingMeasurementChangeData(MeasurementType.Length, data, Title, Description);
        }

        public void SetPixelSpacing(double? pixelSpacingX, double? pixelSpacingY)
        {
            PixelSpacingX = pixelSpacingX;
            PixelSpacingY = pixelSpacingY;
        }

        public void SetPreviewEndPoint(double imageX, double imageY)
        {
            PreviewEndImageX = imageX;
            PreviewEndImageY = imageY;
        }

        private static double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double CalculatePhysicalDistance(double x1, double y1, double x2, double y2, double pixelSpacingX, double pixelSpacingY)
        {
            var dxMillimeters = (x2 - x1) * pixelSpacingX;
            var dyMillimeters = (y2 - y1) * pixelSpacingY;

            return Math.Sqrt(dxMillimeters * dxMillimeters + dyMillimeters * dyMillimeters);
        }

        private void RaiseMeasurementPropertiesChanged()
        {
            RaisePropertyChanged(nameof(IsComplete));
            RaisePropertyChanged(nameof(IsDraft));
            RaisePropertyChanged(nameof(DistancePixels));
            RaisePropertyChanged(nameof(DistanceMillimeters));
            RaisePropertyChanged(nameof(LabelText));
            RaisePropertyChanged(nameof(DisplayTitle));
        }

        private void RaisePreviewPropertiesChanged()
        {
            RaisePropertyChanged(nameof(HasPreviewEndPoint));
            RaisePropertyChanged(nameof(PreviewDistancePixels));
            RaisePropertyChanged(nameof(PreviewDistanceMillimeters));
            RaisePropertyChanged(nameof(PreviewLabelText));
        }

        private static DateTime NormalizeUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

        private void Touch() => ModifiedAt = DateTime.UtcNow;
    }
}
