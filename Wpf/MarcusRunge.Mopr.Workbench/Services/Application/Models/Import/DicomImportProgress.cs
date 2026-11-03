namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.Import
{
    /// <summary>
    /// Reports non-sensitive progress for one application DICOM import operation.
    /// </summary>
    public sealed record DicomImportProgress(int TotalFiles, int ProcessedFiles, int ImportedFiles, int ExistingFiles, int SkippedFiles, int FailedFiles)
    {
        public double Percentage => TotalFiles <= 0 ? 0d : Math.Min(100d, ProcessedFiles * 100d / TotalFiles);
    }
}