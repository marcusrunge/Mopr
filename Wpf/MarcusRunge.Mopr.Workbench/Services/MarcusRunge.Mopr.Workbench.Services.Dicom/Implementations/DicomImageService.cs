using FellowOakDicom;
using FellowOakDicom.Imaging;
using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Implementations
{
    internal class DicomImageService : CreateableBindableBase<IDicomImageService, DicomImageService, IDicomBase>, IDicomImageService
    {
        private IDicomBase? _base;

        public async Task<DicomGrayscaleImage?> LoadGrayscaleImageAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var dicomFile = await DicomFile.OpenAsync(filePath).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                var dataset = dicomFile.Dataset;
                var pixelData = DicomPixelData.Create(dataset);

                var width = pixelData.Width;
                var height = pixelData.Height;

                if (width <= 0 || height <= 0)
                {
                    return null;
                }

                var frame = pixelData.GetFrame(0);
                var frameData = frame.Data;

                if (frameData == null || frameData.Length == 0)
                {
                    return null;
                }

                var pixels = ConvertToGrayscale8(dataset, pixelData, frameData, width, height);

                return new DicomGrayscaleImage(filePath, width, height, pixels);
            }
            catch (Exception exception)
            {
                _base?.OnExceptionThrown(exception);
                return null;
            }
        }

        protected override void OnCreate(IDicomBase @base) => _base = @base;

        protected override Task OnCreateAsync(IDicomBase @base, CancellationToken cancellationToken)
        {
            _base = @base;
            return Task.CompletedTask;
        }

        private static byte ApplyWindow(double value, double? windowCenter, double? windowWidth)
        {
            if (windowCenter.HasValue && windowWidth.HasValue && windowWidth.Value > 1)
            {
                var min = windowCenter.Value - windowWidth.Value / 2.0;
                var max = windowCenter.Value + windowWidth.Value / 2.0;

                if (value <= min)
                {
                    return 0;
                }

                if (value >= max)
                {
                    return 255;
                }

                return (byte)Math.Round((value - min) / (max - min) * 255.0);
            }

            if (value <= 0)
            {
                return 0;
            }

            if (value >= 255)
            {
                return 255;
            }

            return (byte)Math.Round(value);
        }

        private static void Convert16Bit(byte[] source, byte[] target, int pixelCount, bool signed, double slope, double intercept, double? windowCenter, double? windowWidth)
        {
            var availablePixels = Math.Min(pixelCount, source.Length / 2);

            for (var i = 0; i < availablePixels; i++)
            {
                var byteIndex = i * 2;

                int rawValue = signed ? (int)BitConverter.ToInt16(source, byteIndex) : (int)BitConverter.ToUInt16(source, byteIndex);

                var value = rawValue * slope + intercept;

                target[i] = ApplyWindow(value, windowCenter, windowWidth);
            }
        }

        private static void Convert8Bit(byte[] source, byte[] target, int pixelCount, double slope, double intercept, double? windowCenter, double? windowWidth)
        {
            var count = Math.Min(pixelCount, source.Length);

            for (var i = 0; i < count; i++)
            {
                var value = source[i] * slope + intercept;
                target[i] = ApplyWindow(value, windowCenter, windowWidth);
            }
        }

        private static byte[] ConvertToGrayscale8(DicomDataset dataset, DicomPixelData pixelData, byte[] frameData, int width, int height)
        {
            var pixelCount = width * height;
            var output = new byte[pixelCount];

            var bitsAllocated = pixelData.BitsAllocated;
            var pixelRepresentation = GetInt(dataset, DicomTag.PixelRepresentation) ?? 0;

            var slope = GetDouble(dataset, DicomTag.RescaleSlope) ?? 1.0;
            var intercept = GetDouble(dataset, DicomTag.RescaleIntercept) ?? 0.0;

            var windowCenter = GetDouble(dataset, DicomTag.WindowCenter);
            var windowWidth = GetDouble(dataset, DicomTag.WindowWidth);

            if (bitsAllocated <= 8)
            {
                Convert8Bit(frameData, output, pixelCount, slope, intercept, windowCenter, windowWidth);

                return output;
            }

            Convert16Bit(frameData, output, pixelCount, pixelRepresentation != 0, slope, intercept, windowCenter, windowWidth);

            return output;
        }

        private static double? GetDouble(DicomDataset dataset, DicomTag tag)
        {
            if (dataset.TryGetSingleValue<double>(tag, out var doubleValue))
            {
                return doubleValue;
            }

            if (dataset.TryGetSingleValue<float>(tag, out var floatValue))
            {
                return floatValue;
            }

            if (dataset.TryGetSingleValue<decimal>(tag, out var decimalValue))
            {
                return (double)decimalValue;
            }

            if (dataset.TryGetSingleValue<string>(tag, out var stringValue) &&
                double.TryParse(stringValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedValue))
            {
                return parsedValue;
            }

            return null;
        }

        private static int? GetInt(DicomDataset dataset, DicomTag tag)
        {
            if (dataset.TryGetSingleValue<int>(tag, out var intValue))
            {
                return intValue;
            }

            if (dataset.TryGetSingleValue<ushort>(tag, out var ushortValue))
            {
                return ushortValue;
            }

            if (dataset.TryGetSingleValue<short>(tag, out var shortValue))
            {
                return shortValue;
            }

            if (dataset.TryGetSingleValue<string>(tag, out var stringValue) &&
                int.TryParse(stringValue, out var parsedValue))
            {
                return parsedValue;
            }

            return null;
        }
    }
}