namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts
{
    public sealed class DicomGrayscaleImage
    {
        public DicomGrayscaleImage(string filePath, int width, int height, byte[] pixels)
        {
            FilePath = filePath;
            Width = width;
            Height = height;
            Pixels = pixels;
        }

        public string FilePath { get; }

        public int Height { get; }

        /// <summary>
        /// 8-bit grayscale pixels, one byte per pixel.
        /// </summary>
        public byte[] Pixels { get; }

        public int Width { get; }
    }
}