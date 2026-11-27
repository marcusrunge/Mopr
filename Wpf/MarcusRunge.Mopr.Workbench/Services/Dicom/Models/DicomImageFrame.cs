namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Models
{
    public sealed class DicomImageFrame
    {
        public DicomImageFrame(string filePath, string? sopInstanceUid, int frameIndex, int frameCount, int width, int height, double[] values, double? defaultWindowCenter, double? defaultWindowWidth, string? photometricInterpretation, string? modality)
        {
            FilePath = filePath;
            SopInstanceUid = sopInstanceUid;
            FrameIndex = frameIndex;
            FrameCount = frameCount;
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
        public int FrameCount { get; }
        public int FrameIndex { get; }
        public bool HasDefaultWindow => DefaultWindowCenter.HasValue && DefaultWindowWidth.HasValue && DefaultWindowWidth.Value > 1;
        public int Height { get; }
        public string? Modality { get; }
        public string? PhotometricInterpretation { get; }
        public int PixelCount => Width * Height;
        public string? SopInstanceUid { get; }

        /// <summary>
        /// Gets the original or rescaled pixel values. CT values are normally HU-like after applying Rescale Slope and Rescale Intercept.
        /// </summary>
        public double[] Values { get; }

        public int Width { get; }
    }
}
