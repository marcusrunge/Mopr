using FellowOakDicom;
using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using System;
using System.IO;
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

                if (!fileInfo.Exists || fileInfo.Length < 132)
                {
                    return false;
                }

                using var stream = File.OpenRead(filePath);

                var buffer = new byte[132];
                var read = stream.Read(buffer, 0, buffer.Length);

                if (read < 132)
                {
                    return false;
                }

                return buffer[128] == (byte)'D' && buffer[129] == (byte)'I' && buffer[130] == (byte)'C' && buffer[131] == (byte)'M';
            }
            catch (Exception exception)
            {
                _base?.OnExceptionThrown(exception);
                return false;
            }
        }

        public async Task<DicomFileMetadata?> ReadMetadataAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!IsDicomFile(filePath))
            {
                return null;
            }

            try
            {
                var dicomFile = await DicomFile.OpenAsync(filePath).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                var dataset = dicomFile.Dataset;

                return new DicomFileMetadata(filePath: filePath, studyInstanceUid: GetString(dataset, DicomTag.StudyInstanceUID), seriesInstanceUid: GetString(dataset, DicomTag.SeriesInstanceUID), sopInstanceUid: GetString(dataset, DicomTag.SOPInstanceUID), modality: GetString(dataset, DicomTag.Modality), studyDescription: GetString(dataset, DicomTag.StudyDescription), seriesDescription: GetString(dataset, DicomTag.SeriesDescription), instanceNumber: GetInt(dataset, DicomTag.InstanceNumber), rows: GetInt(dataset, DicomTag.Rows), columns: GetInt(dataset, DicomTag.Columns));
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

            if (dataset.TryGetSingleValue<string>(tag, out var stringValue) && int.TryParse(stringValue, out var parsedValue))
            {
                return parsedValue;
            }

            return null;
        }

        private static string? GetString(DicomDataset dataset, DicomTag tag) => dataset.TryGetSingleValue<string>(tag, out var value) ? string.IsNullOrWhiteSpace(value) ? null : value : null;
    }
}