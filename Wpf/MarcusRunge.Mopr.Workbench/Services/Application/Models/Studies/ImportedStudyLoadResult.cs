namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies
{
    /// <summary>
    /// Represents the atomically validated repository files of one imported study selection.
    /// </summary>
    public sealed class ImportedStudyLoadResult
    {
        private ImportedStudyLoadResult(bool isSuccessful, string? studyInstanceUid, IReadOnlyList<ImportedSeriesLoadResult> series, IReadOnlyList<string> technicalErrors)
        {
            IsSuccessful = isSuccessful;
            StudyInstanceUid = studyInstanceUid;
            Series = series;
            TechnicalErrors = technicalErrors;
        }

        public bool IsSuccessful { get; }
        public IReadOnlyList<ImportedSeriesLoadResult> Series { get; }
        public string? StudyInstanceUid { get; }
        public IReadOnlyList<string> TechnicalErrors { get; }

        public static ImportedStudyLoadResult Failed(params string[] technicalErrors) => new(false, null, [], technicalErrors.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray());
        public static ImportedStudyLoadResult Succeeded(string studyInstanceUid, IReadOnlyList<ImportedSeriesLoadResult> series) => new(true, studyInstanceUid, series, Array.Empty<string>());
    }
}