namespace MarcusRunge.Mopr.Workbench.Contracts.Imaging
{
    public sealed class ImagingFolderScanSummary
    {
        public ImagingFolderScanSummary(string folderPath, int totalFiles, int dicomCandidates, int validDicomFiles, int imageFiles, int otherFiles)
        {
            FolderPath = folderPath;
            TotalFiles = totalFiles;
            DicomCandidates = dicomCandidates;
            ValidDicomFiles = validDicomFiles;
            ImageFiles = imageFiles;
            OtherFiles = otherFiles;
        }

        public int DicomCandidates { get; }
        public string DisplayText => $"{ValidDicomFiles} DICOM-Dateien, {DicomCandidates} Kandidaten, {TotalFiles} Dateien gesamt";

        public string FolderPath { get; }

        public int ImageFiles { get; }
        public int OtherFiles { get; }
        public int TotalFiles { get; }
        public int ValidDicomFiles { get; }
    }
}