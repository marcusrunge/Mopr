using FellowOakDicom;
using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Implementations
{
    internal sealed class DicomMetadataService : CreateableBindableBase<IDicomMetadataService, DicomMetadataService, IDicomBase>, IDicomMetadataService
    {
        private IDicomBase? _base;

        public bool IsDicomFile(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists || fileInfo.Length < 132) return false;

                using var stream = File.OpenRead(filePath);
                var buffer = new byte[132];
                var read = stream.Read(buffer, 0, buffer.Length);
                return read == 132 && buffer[128] == (byte)'D' && buffer[129] == (byte)'I' && buffer[130] == (byte)'C' && buffer[131] == (byte)'M';
            }
            catch (Exception exception)
            {
                _base?.OnExceptionThrown(exception);
                return false;
            }
        }

        public async Task<DicomFileMetadata?> ReadMetadataAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return null;

            cancellationToken.ThrowIfCancellationRequested();
            if (!IsDicomFile(filePath)) return null;

            try
            {
                var dicomFile = await DicomFile.OpenAsync(filePath).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                var dataset = dicomFile.Dataset;

                return new DicomFileMetadata(
                    filePath: filePath,
                    studyInstanceUid: GetString(dataset, DicomTag.StudyInstanceUID),
                    seriesInstanceUid: GetString(dataset, DicomTag.SeriesInstanceUID),
                    sopInstanceUid: GetString(dataset, DicomTag.SOPInstanceUID),
                    modality: GetString(dataset, DicomTag.Modality),
                    studyDescription: GetString(dataset, DicomTag.StudyDescription),
                    seriesDescription: GetString(dataset, DicomTag.SeriesDescription),
                    instanceNumber: GetInt(dataset, DicomTag.InstanceNumber),
                    rows: GetInt(dataset, DicomTag.Rows),
                    columns: GetInt(dataset, DicomTag.Columns),
                    patientName: NormalizePersonName(GetString(dataset, DicomTag.PatientName)),
                    patientBirthDate: GetDicomDate(dataset, DicomTag.PatientBirthDate),
                    patientId: GetString(dataset, DicomTag.PatientID),
                    studyDate: GetStudyDateTime(dataset),
                    accessionNumber: GetString(dataset, DicomTag.AccessionNumber),
                    seriesNumber: GetInt(dataset, DicomTag.SeriesNumber));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
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

        private static DateTime? GetDicomDate(DicomDataset dataset, DicomTag tag)
        {
            var value = GetString(dataset, tag);
            return DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date.Date : (DateTime?)null;
        }

        private static int? GetInt(DicomDataset dataset, DicomTag tag)
        {
            if (dataset.TryGetSingleValue<int>(tag, out var intValue)) return intValue;
            if (dataset.TryGetSingleValue<ushort>(tag, out var ushortValue)) return ushortValue;
            if (dataset.TryGetSingleValue<short>(tag, out var shortValue)) return shortValue;

            return dataset.TryGetSingleValue<string>(tag, out var stringValue) && int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue) ? parsedValue : (int?)null;
        }

        private static string? GetString(DicomDataset dataset, DicomTag tag) => dataset.TryGetSingleValue<string>(tag, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

        private static DateTime? GetStudyDateTime(DicomDataset dataset)
        {
            var date = GetDicomDate(dataset, DicomTag.StudyDate);
            if (date is null) return null;

            var timeValue = GetString(dataset, DicomTag.StudyTime);
            if (string.IsNullOrWhiteSpace(timeValue)) return date;

            var normalizedTime = timeValue.Split('.')[0].PadRight(6, '0');
            if (normalizedTime.Length < 6 || !int.TryParse(normalizedTime[..2], NumberStyles.None, CultureInfo.InvariantCulture, out var hour) || !int.TryParse(normalizedTime.Substring(2, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var minute) || !int.TryParse(normalizedTime.Substring(4, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var second)) return date;

            try
            {
                return date.Value.Add(new TimeSpan(hour, minute, second));
            }
            catch (ArgumentOutOfRangeException)
            {
                return date;
            }
        }

        private static string? NormalizePersonName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            // DICOM PN components are ordered as family, given, middle, prefix and suffix.
            // Missing components are omitted without inventing patient information.
            var components = value.Split('^').Select(component => component.Trim()).ToArray();
            var familyName = components.ElementAtOrDefault(0);
            var remainingNames = string.Join(" ", components.Skip(1).Where(component => !string.IsNullOrWhiteSpace(component)));
            if (string.IsNullOrWhiteSpace(familyName)) return string.IsNullOrWhiteSpace(remainingNames) ? null : remainingNames;
            return string.IsNullOrWhiteSpace(remainingNames) ? familyName : $"{familyName}, {remainingNames}";
        }
    }
}
