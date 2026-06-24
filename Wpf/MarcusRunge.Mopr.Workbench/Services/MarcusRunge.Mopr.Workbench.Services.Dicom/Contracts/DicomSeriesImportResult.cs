using System.Collections.Generic;

namespace MarcusRunge.Mopr.Workbench.Services.Dicom.Contracts
{
    public sealed class DicomSeriesImportResult
    {
        public DicomSeriesImportResult(string seriesInstanceUid, string? modality, string? seriesDescription, IReadOnlyList<DicomFileMetadata> files)
        {
            SeriesInstanceUid = seriesInstanceUid;
            Modality = modality;
            SeriesDescription = seriesDescription;
            Files = files;
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(SeriesDescription))
                {
                    return SeriesDescription;
                }

                if (!string.IsNullOrWhiteSpace(Modality))
                {
                    return Modality + " Serie";
                }

                return "DICOM-Serie";
            }
        }

        public IReadOnlyList<DicomFileMetadata> Files { get; }
        public int InstanceCount => Files.Count;
        public string? Modality { get; }
        public string? SeriesDescription { get; }
        public string SeriesInstanceUid { get; }
    }
}