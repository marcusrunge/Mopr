namespace MarcusRunge.Mopr.Workbench.Contracts.Imaging
{
    public sealed class ImagingStudyLoadProgress(string message, int processedFiles, int totalFiles)
    {
        public string DisplayText => TotalFiles <= 0 ? Message : $"{Message} ({ProcessedFiles}/{TotalFiles})";
        public string Message { get; } = message;
        public int ProcessedFiles { get; } = processedFiles;
        public double Progress => TotalFiles <= 0 ? 0 : (double)ProcessedFiles / TotalFiles;
        public int TotalFiles { get; } = totalFiles;
    }
}