namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.Studies
{
    /// <summary>
    /// Represents the validated physical files of one imported DICOM series.
    /// </summary>
    public sealed class ImportedSeriesLoadResult
    {
        private ImportedSeriesLoadResult(bool isSuccessful, int seriesId, string? seriesInstanceUid, string? studyInstanceUid, IReadOnlyList<string> files, IReadOnlyList<string> technicalErrors)
        {
            IsSuccessful = isSuccessful;
            SeriesId = seriesId;
            SeriesInstanceUid = seriesInstanceUid;
            StudyInstanceUid = studyInstanceUid;
            Files = files;
            TechnicalErrors = technicalErrors;
        }

        /// <summary>
        /// Gets the validated absolute DICOM file paths in deterministic instance order.
        /// </summary>
        public IReadOnlyList<string> Files { get; }

        /// <summary>
        /// Gets a value indicating whether the complete series was resolved safely.
        /// </summary>
        public bool IsSuccessful { get; }

        /// <summary>
        /// Gets the persisted series ID.
        /// </summary>
        public int SeriesId { get; }

        /// <summary>
        /// Gets the DICOM series instance UID.
        /// </summary>
        public string? SeriesInstanceUid { get; }

        /// <summary>
        /// Gets the DICOM study instance UID.
        /// </summary>
        public string? StudyInstanceUid { get; }

        /// <summary>
        /// Gets technical diagnostics that must not be displayed as unfiltered user messages.
        /// </summary>
        public IReadOnlyList<string> TechnicalErrors { get; }

        /// <summary>
        /// Creates a failed result without exposing technical diagnostics to the UI directly.
        /// </summary>
        public static ImportedSeriesLoadResult Failed(int seriesId, string? seriesInstanceUid, params string[] technicalErrors) => new(false, seriesId, seriesInstanceUid, null, [], [.. technicalErrors.Where(error => !string.IsNullOrWhiteSpace(error))]);

        /// <summary>
        /// Creates a successful result for a completely validated series.
        /// </summary>
        public static ImportedSeriesLoadResult Succeeded(int seriesId, string seriesInstanceUid, string studyInstanceUid, IReadOnlyList<string> files) => new(true, seriesId, seriesInstanceUid, studyInstanceUid, files, []);
    }
}