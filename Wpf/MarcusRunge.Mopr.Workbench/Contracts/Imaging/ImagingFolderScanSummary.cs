namespace MarcusRunge.Mopr.Workbench.Contracts.Imaging
{
    public sealed class ImagingFolderScanSummary(string folderPath, int totalFiles, int dicomCandidates, int validDicomFiles, int imageFiles, int otherFiles)
    {
        public int DicomCandidates { get; } = dicomCandidates;
        public string DisplayText => $"{ValidDicomFiles} DICOM-Dateien, {DicomCandidates} Kandidaten, {TotalFiles} Dateien gesamt";

        public string FolderPath { get; } = folderPath;

        public int ImageFiles { get; } = imageFiles;
        public int OtherFiles { get; } = otherFiles;
        public int TotalFiles { get; } = totalFiles;
        public int ValidDicomFiles { get; } = validDicomFiles;
    }
}