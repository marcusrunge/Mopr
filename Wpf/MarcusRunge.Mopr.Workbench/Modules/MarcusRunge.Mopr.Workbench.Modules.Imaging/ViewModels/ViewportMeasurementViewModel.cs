using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ViewportMeasurementViewModel(double startImageX, double startImageY) : ViewModelBase
    {
        private double? _endImageX;
        private double? _endImageY;
        private double? _previewEndImageX;
        private double? _previewEndImageY;

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

        public bool HasPreviewEndPoint => PreviewEndImageX.HasValue && PreviewEndImageY.HasValue;
        public Guid Id { get; } = Guid.NewGuid();

        public bool IsComplete => EndImageX.HasValue && EndImageY.HasValue;
        public bool IsDraft => !IsComplete;

        public string LabelText
        {
            get
            {
                if (!DistancePixels.HasValue)
                {
                    return string.Empty;
                }

                return string.Format(Resources.Measurement_LabelPixelFormat, DistancePixels.Value);
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
                if (!PreviewDistancePixels.HasValue)
                {
                    return string.Empty;
                }

                return string.Format(Resources.Measurement_LabelPixelFormat, PreviewDistancePixels.Value);
            }
        }

        public double StartImageX { get; } = startImageX;

        public double StartImageY { get; } = startImageY;

        public void Complete(double imageX, double imageY)
        {
            EndImageX = imageX;
            EndImageY = imageY;

            PreviewEndImageX = null;
            PreviewEndImageY = null;

            RaiseMeasurementPropertiesChanged();
            RaisePreviewPropertiesChanged();
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

        private void RaiseMeasurementPropertiesChanged()
        {
            RaisePropertyChanged(nameof(IsComplete));
            RaisePropertyChanged(nameof(IsDraft));
            RaisePropertyChanged(nameof(DistancePixels));
            RaisePropertyChanged(nameof(LabelText));
        }

        private void RaisePreviewPropertiesChanged()
        {
            RaisePropertyChanged(nameof(HasPreviewEndPoint));
            RaisePropertyChanged(nameof(PreviewDistancePixels));
            RaisePropertyChanged(nameof(PreviewLabelText));
        }
    }
}