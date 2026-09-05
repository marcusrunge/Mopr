using MarcusRunge.Mopr.Workbench.Contracts.Dicom.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarcusRunge.Mopr.Workbench.Contracts.Dicom.Models
{
    /// <summary>
    /// Represents the application-oriented result of a DICOM import operation.
    /// </summary>
    public sealed record DicomImportResult
    {
        private readonly IReadOnlyList<string> _technicalErrors;

        /// <summary>
        /// Initializes a new instance of the <see cref="DicomImportResult"/> class.
        /// </summary>
        /// <param name="status">The structured import status.</param>
        /// <param name="discoveredFiles">The number of discovered source files.</param>
        /// <param name="validDicomFiles">The number of valid DICOM files.</param>
        /// <param name="importableFiles">The number of importable DICOM files.</param>
        /// <param name="importedFiles">The number of imported files.</param>
        /// <param name="skippedFiles">The number of skipped files.</param>
        /// <param name="failedFiles">The number of files that failed to import.</param>
        /// <param name="technicalErrors">Technical diagnostics that must not be displayed as unfiltered user messages.</param>
        public DicomImportResult(DicomImportStatus status, int discoveredFiles = 0, int validDicomFiles = 0, int importableFiles = 0, int importedFiles = 0, int skippedFiles = 0, int failedFiles = 0, IEnumerable<string>? technicalErrors = null)
        {
            Status = status;
            DiscoveredFiles = discoveredFiles;
            ValidDicomFiles = validDicomFiles;
            ImportableFiles = importableFiles;
            ImportedFiles = importedFiles;
            SkippedFiles = skippedFiles;
            FailedFiles = failedFiles;
            _technicalErrors = technicalErrors?.Where(error => !string.IsNullOrWhiteSpace(error)).ToArray() ?? [];
        }

        /// <summary>
        /// Gets the number of discovered source files.
        /// </summary>
        public int DiscoveredFiles { get; }

        /// <summary>
        /// Gets the number of files that failed to import.
        /// </summary>
        public int FailedFiles { get; }

        /// <summary>
        /// Gets the number of importable DICOM files.
        /// </summary>
        public int ImportableFiles { get; }

        /// <summary>
        /// Gets the number of imported files.
        /// </summary>
        public int ImportedFiles { get; }

        /// <summary>
        /// Gets a value indicating whether the complete operation succeeded without failed files.
        /// </summary>
        public bool IsSuccessful => Status is DicomImportStatus.Completed or DicomImportStatus.CompletedWithSkippedFiles;

        /// <summary>
        /// Gets the number of skipped files.
        /// </summary>
        public int SkippedFiles { get; }

        /// <summary>
        /// Gets the structured import status.
        /// </summary>
        public DicomImportStatus Status { get; }

        /// <summary>
        /// Gets technical diagnostics that must not be displayed as unfiltered user messages.
        /// </summary>
        public IReadOnlyList<string> TechnicalErrors => _technicalErrors;

        /// <summary>
        /// Gets the number of valid DICOM files.
        /// </summary>
        public int ValidDicomFiles { get; }

        /// <summary>
        /// Creates a result without importing any files.
        /// </summary>
        /// <param name="status">The prerequisite or cancellation status.</param>
        /// <returns>The structured result.</returns>
        public static DicomImportResult WithoutImport(DicomImportStatus status) => new(status);

        /// <summary>
        /// Creates a failed result containing separated technical diagnostics.
        /// </summary>
        /// <param name="exception">The technical failure.</param>
        /// <returns>The failed result.</returns>
        public static DicomImportResult Failed(Exception exception) => new(DicomImportStatus.Failed, technicalErrors: new[] { (exception ?? throw new ArgumentNullException(nameof(exception))).ToString() });
    }
}