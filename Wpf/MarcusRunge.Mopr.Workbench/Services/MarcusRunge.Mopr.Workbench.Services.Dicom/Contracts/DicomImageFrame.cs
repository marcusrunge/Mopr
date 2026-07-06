namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts
{
    public sealed class DicomImageFrame
    {
        public DicomImageFrame(string filePath, int width, int height, double[] values, double? defaultWindowCenter, double? defaultWindowWidth, string? photometricInterpretation, string? modality)
        {
            FilePath = filePath;
            Width = width;
            Height = height;
            Values = values;
            DefaultWindowCenter = defaultWindowCenter;
            DefaultWindowWidth = defaultWindowWidth;
            PhotometricInterpretation = photometricInterpretation;
            Modality = modality;
        }
        public double? PixelSpacingX { get; set; }

        public double? PixelSpacingY { get; set; }
        public double? DefaultWindowCenter { get; }
        public double? DefaultWindowWidth { get; }
        public string FilePath { get; }

        public bool HasDefaultWindow => DefaultWindowCenter.HasValue && DefaultWindowWidth.HasValue && DefaultWindowWidth.Value > 1;

        public int Height { get; }
        public string? Modality { get; }
        public string? PhotometricInterpretation { get; }
        public int PixelCount => Width * Height;

        /// <summary>
        /// Original or rescaled pixel values.
        /// For CT this should usually be HU-like values after RescaleSlope/RescaleIntercept.
        /// </summary>
        public double[] Values { get; }

        public int Width { get; }
    }
}