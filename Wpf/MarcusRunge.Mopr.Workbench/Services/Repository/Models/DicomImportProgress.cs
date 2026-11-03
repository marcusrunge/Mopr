namespace MarcusRunge.Mopr.Workbench.Services.Repository.Models
{
    /// <summary>
    /// Reports non-sensitive progress for one repository DICOM import operation.
    /// </summary>
    public sealed record DicomImportProgress(int TotalFiles, int ProcessedFiles, int ImportedFiles, int ExistingFiles, int SkippedFiles, int FailedFiles, bool IsInspectingSource);
}