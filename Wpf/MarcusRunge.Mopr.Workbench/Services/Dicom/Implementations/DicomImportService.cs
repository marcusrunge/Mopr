using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Implementations
{
    internal sealed class DicomImportService : CreateableBindableBase<IDicomImportService, DicomImportService, IDicomBase>, IDicomImportService
    {
        private IDicomBase? _base;

        public async Task<DicomImportResult?> ImportFolderAsync(string folderPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var metadataService = _base?.Dicom?.MetadataService;

            if (metadataService == null)
            {
                return null;
            }

            try
            {
                var allFiles = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories).ToList();

                var metadataItems = new List<DicomFileMetadata>();

                foreach (var file in allFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!metadataService.IsDicomFile(file))
                    {
                        continue;
                    }

                    var metadata = await metadataService.ReadMetadataAsync(file, cancellationToken).ConfigureAwait(false);

                    if (metadata != null)
                    {
                        metadataItems.Add(metadata);
                    }
                }

                if (metadataItems.Count == 0)
                {
                    return new DicomImportResult(folderPath: folderPath, studyInstanceUid: null, studyDescription: null, series: Array.Empty<DicomSeriesImportResult>());
                }

                var first = metadataItems[0];

                var groupedSeries = metadataItems.GroupBy(GetSeriesGroupingKey).Select(group =>
                {
                    var orderedFiles = group.OrderBy(item => item.InstanceNumber ?? int.MaxValue).ThenBy(item => item.FilePath, StringComparer.OrdinalIgnoreCase).ToList();

                    var firstInSeries = orderedFiles[0];

                    return new DicomSeriesImportResult(seriesInstanceUid: firstInSeries.SeriesInstanceUid ?? group.Key, modality: firstInSeries.Modality, seriesDescription: firstInSeries.SeriesDescription, files: orderedFiles);
                }).OrderBy(series => series.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();

                return new DicomImportResult(folderPath: folderPath, studyInstanceUid: first.StudyInstanceUid, studyDescription: first.StudyDescription, series: groupedSeries);
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

        protected override void OnCreate(IDicomBase @base) => _base = @base;

        protected override Task OnCreateAsync(IDicomBase @base, CancellationToken cancellationToken)
        {
            _base = @base;
            return Task.CompletedTask;
        }

        private static string GetSeriesGroupingKey(DicomFileMetadata metadata)
        {
            if (!string.IsNullOrWhiteSpace(metadata.SeriesInstanceUid))
            {
                return metadata.SeriesInstanceUid;
            }

            if (!string.IsNullOrWhiteSpace(metadata.SeriesDescription))
            {
                return "description:" + metadata.SeriesDescription;
            }

            if (!string.IsNullOrWhiteSpace(metadata.Modality))
            {
                return "modality:" + metadata.Modality;
            }

            return "unknown-series";
        }
    }
}