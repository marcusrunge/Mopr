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

        public async Task<DicomGrayscaleImage?> LoadGrayscaleImageAsync(string filePath, double? windowCenter = null, double? windowWidth = null, CancellationToken cancellationToken = default)
        {
            var frame = await LoadImageFrameAsync(filePath, cancellationToken);

            if (frame == null)
            {
                return null;
            }

            return RenderGrayscaleImage(frame, windowCenter, windowWidth);
        }

        public async Task<DicomImageFrame?> LoadImageFrameAsync(string filePath, CancellationToken cancellationToken = default)
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

                var values = ConvertToValues(dataset, pixelData, frameData, width, height);

                var defaultWindowCenter = GetDouble(dataset, DicomTag.WindowCenter);

                var defaultWindowWidth = GetDouble(dataset, DicomTag.WindowWidth);

                var photometricInterpretation = GetString(dataset, DicomTag.PhotometricInterpretation);
                var modality = GetString(dataset, DicomTag.Modality);

                var pixelSpacing = GetPixelSpacing(dataset);

                var imageFrame = new DicomImageFrame(filePath: filePath, width: width, height: height, values: values, defaultWindowCenter: defaultWindowCenter, defaultWindowWidth: defaultWindowWidth, photometricInterpretation: photometricInterpretation, modality: modality);

                if (pixelSpacing.HasValue)
                {
                    imageFrame.PixelSpacingY = pixelSpacing.Value.RowSpacing;
                    imageFrame.PixelSpacingX = pixelSpacing.Value.ColumnSpacing;
                }

                return imageFrame;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _base?.OnExceptionThrown(exception);
                return null;
            }
        }

        public DicomGrayscaleImage? RenderGrayscaleImage(DicomImageFrame frame, double? windowCenter = null, double? windowWidth = null)
        {
            if (frame == null)
            {
                return null;
            }

            if (frame.Width <= 0 || frame.Height <= 0)
            {
                return null;
            }

            if (frame.Values.Length < frame.Width * frame.Height)
            {
                return null;
            }

            var effectiveWindowCenter = windowCenter ?? frame.DefaultWindowCenter;

            var effectiveWindowWidth = windowWidth ?? frame.DefaultWindowWidth;

            var pixels = new byte[frame.Width * frame.Height];

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = ApplyWindow(frame.Values[i], effectiveWindowCenter, effectiveWindowWidth);
            }

            if (string.Equals(frame.PhotometricInterpretation, "MONOCHROME1", StringComparison.OrdinalIgnoreCase))
            {
                Invert(pixels);
            }

            return new DicomGrayscaleImage(frame.FilePath, frame.Width, frame.Height, pixels);
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

        private static void Convert16BitToValues(byte[] source, double[] target, int pixelCount, bool signed, double slope, double intercept)
        {
            var availablePixels = Math.Min(pixelCount, source.Length / 2);

            for (var i = 0; i < availablePixels; i++)
            {
                var byteIndex = i * 2;

                int rawValue;

                if (signed)
                {
                    rawValue = BitConverter.ToInt16(source, byteIndex);
                }
                else
                {
                    rawValue = BitConverter.ToUInt16(source, byteIndex);
                }

                target[i] = rawValue * slope + intercept;
            }
        }

        private static void Convert8BitToValues(byte[] source, double[] target, int pixelCount, double slope, double intercept)
        {
            var count = Math.Min(pixelCount, source.Length);

            for (var i = 0; i < count; i++)
            {
                target[i] = source[i] * slope + intercept;
            }
        }

        private static double[] ConvertToValues(DicomDataset dataset, DicomPixelData pixelData, byte[] frameData, int width, int height)
        {
            var pixelCount = width * height;
            var values = new double[pixelCount];

            var bitsAllocated = pixelData.BitsAllocated;
            var pixelRepresentation = GetInt(dataset, DicomTag.PixelRepresentation) ?? 0;

            var slope = GetDouble(dataset, DicomTag.RescaleSlope) ?? 1.0;

            var intercept = GetDouble(dataset, DicomTag.RescaleIntercept) ?? 0.0;

            if (bitsAllocated <= 8)
            {
                Convert8BitToValues(frameData, values, pixelCount, slope, intercept);

                return values;
            }

            Convert16BitToValues(frameData, values, pixelCount, pixelRepresentation != 0, slope, intercept);

            return values;
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

            if (dataset.TryGetSingleValue<string>(tag, out var stringValue) && double.TryParse(stringValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsedValue))
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

        private static PixelSpacingInfo? GetPixelSpacing(DicomDataset dataset)
        {
            if (dataset.TryGetValues<double>(DicomTag.PixelSpacing, out var doubleValues) &&
                doubleValues.Length >= 2 && doubleValues[0] > 0 && doubleValues[1] > 0)
            {
                return new PixelSpacingInfo(rowSpacing: doubleValues[0], columnSpacing: doubleValues[1]);
            }

            if (dataset.TryGetValues<float>(DicomTag.PixelSpacing, out var floatValues) && floatValues.Length >= 2 && floatValues[0] > 0 && floatValues[1] > 0)
            {
                return new PixelSpacingInfo(rowSpacing: floatValues[0], columnSpacing: floatValues[1]);
            }

            if (dataset.TryGetValues<decimal>(DicomTag.PixelSpacing, out var decimalValues) && decimalValues.Length >= 2 && decimalValues[0] > 0 && decimalValues[1] > 0)
            {
                return new PixelSpacingInfo(rowSpacing: (double)decimalValues[0], columnSpacing: (double)decimalValues[1]);
            }

            if (dataset.TryGetValues<string>(DicomTag.PixelSpacing, out var stringValues) && stringValues.Length >= 2 && double.TryParse(stringValues[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var rowSpacing) && double.TryParse(stringValues[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var columnSpacing) && rowSpacing > 0 && columnSpacing > 0)
            {
                return new PixelSpacingInfo(rowSpacing: rowSpacing, columnSpacing: columnSpacing);
            }

            return null;
        }

        private static string? GetString(DicomDataset dataset, DicomTag tag)
        {
            if (dataset.TryGetSingleValue<string>(tag, out var value))
            {
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }

            return null;
        }

        private static void Invert(byte[] pixels)
        {
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = (byte)(255 - pixels[i]);
            }
        }

        private readonly struct PixelSpacingInfo
        {
            public PixelSpacingInfo(double rowSpacing, double columnSpacing)
            {
                RowSpacing = rowSpacing;
                ColumnSpacing = columnSpacing;
            }

            public double ColumnSpacing { get; }
            public double RowSpacing { get; }
        }
    }
}