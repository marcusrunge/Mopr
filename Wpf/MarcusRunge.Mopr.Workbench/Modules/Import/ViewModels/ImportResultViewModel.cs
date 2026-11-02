using MarcusRunge.Mopr.Workbench.Services.Application.Enums;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Import;

namespace MarcusRunge.Mopr.Workbench.Modules.Import.ViewModels
{
    /// <summary>
    /// Provides the user-facing representation of a DICOM import result.
    /// </summary>
    public sealed class ImportResultViewModel : BindableBase
    {
        private int _discoveredFiles;
        private int _existingFiles;
        private int _failedFiles;
        private int _importableFiles;
        private int _importedFiles;
        private bool _isSuccessful;
        private string _message = string.Empty;
        private int _skippedFiles;
        private DicomImportStatus _status;
        private int _validDicomFiles;

        public int DiscoveredFiles
        {
            get => _discoveredFiles;
            private set => SetProperty(ref _discoveredFiles, value);
        }

        public int ExistingFiles
        {
            get => _existingFiles;
            private set => SetProperty(ref _existingFiles, value);
        }

        public int FailedFiles
        {
            get => _failedFiles;
            private set => SetProperty(ref _failedFiles, value);
        }

        public int ImportableFiles
        {
            get => _importableFiles;
            private set => SetProperty(ref _importableFiles, value);
        }

        public int ImportedFiles
        {
            get => _importedFiles;
            private set => SetProperty(ref _importedFiles, value);
        }

        public bool IsSuccessful
        {
            get => _isSuccessful;
            private set => SetProperty(ref _isSuccessful, value);
        }

        public string Message
        {
            get => _message;
            private set => SetProperty(ref _message, value);
        }

        public int OtherSkippedFiles => Math.Max(0, SkippedFiles - ExistingFiles);

        public int SkippedFiles
        {
            get => _skippedFiles;
            private set => SetProperty(ref _skippedFiles, value);
        }

        public DicomImportStatus Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        public int ValidDicomFiles
        {
            get => _validDicomFiles;
            private set => SetProperty(ref _validDicomFiles, value);
        }

        public void Apply(DicomImportResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            Status = result.Status;
            IsSuccessful = result.IsSuccessful;
            DiscoveredFiles = result.DiscoveredFiles;
            ValidDicomFiles = result.ValidDicomFiles;
            ImportableFiles = result.ImportableFiles;
            ImportedFiles = result.ImportedFiles;
            ExistingFiles = result.ExistingFiles;
            SkippedFiles = result.SkippedFiles;
            RaisePropertyChanged(nameof(OtherSkippedFiles));
            FailedFiles = result.FailedFiles;
            Message = CreateMessage(result);
        }

        private static string CreateMessage(DicomImportResult result)
        {
            if (result.ImportedFiles == 0 && result.FailedFiles == 0 && result.ImportableFiles > 0 && result.ExistingFiles == result.ImportableFiles) return Properties.Resources.ImportResultNoNewImages;
            if (result.ValidDicomFiles == 0) return Properties.Resources.ImportResultNoValidDicomFiles;

            return result.Status switch
            {
                DicomImportStatus.Completed => Properties.Resources.ImportResultCompleted,
                DicomImportStatus.CompletedWithSkippedFiles => Properties.Resources.ImportResultCompletedWithSkippedFiles,
                DicomImportStatus.CompletedWithErrors => Properties.Resources.ImportResultCompletedWithErrors,
                DicomImportStatus.Canceled => Properties.Resources.ImportResultCanceled,
                DicomImportStatus.SourceMissing => Properties.Resources.ImportResultSourceMissing,
                DicomImportStatus.SourceUnavailable => Properties.Resources.ImportResultSourceUnavailable,
                DicomImportStatus.SourceTypeUnsupported => Properties.Resources.ImportResultSourceTypeUnsupported,
                DicomImportStatus.DefaultRepositoryMissing => Properties.Resources.ImportResultDefaultRepositoryMissing,
                DicomImportStatus.RepositoryUnavailable => Properties.Resources.ImportResultRepositoryUnavailable,
                DicomImportStatus.AuditIdentityUnavailable => Properties.Resources.ImportResultAuditIdentityUnavailable,
                _ => Properties.Resources.ImportResultFailed
            };
        }
    }
}