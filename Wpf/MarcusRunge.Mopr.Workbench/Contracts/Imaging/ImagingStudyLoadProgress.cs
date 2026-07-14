namespace MarcusRunge.Mopr.Workbench.Contracts.Imaging
{
    public sealed class ImagingStudyLoadProgress
    {
        public ImagingStudyLoadProgress(string message, int processedFiles, int totalFiles)
        {
            Message = message;
            ProcessedFiles = processedFiles;
            TotalFiles = totalFiles;
        }

        public string DisplayText => TotalFiles <= 0 ? Message : $"{Message} ({ProcessedFiles}/{TotalFiles})";
        public string Message { get; }
        public int ProcessedFiles { get; }
        public double Progress => TotalFiles <= 0 ? 0 : (double)ProcessedFiles / TotalFiles;
        public int TotalFiles { get; }
    }
}