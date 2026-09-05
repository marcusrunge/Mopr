using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Dicom.Enums
{
    /// <summary>
    /// Defines the application-oriented result status of a DICOM import operation.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum DicomImportStatus
    {
        /// <summary>
        /// No import source was specified.
        /// </summary>
        [LocalizedDescription("DicomImportStatus_SourceMissing", typeof(Resources))]
        SourceMissing = 0,

        /// <summary>
        /// The specified import source is not available.
        /// </summary>
        [LocalizedDescription("DicomImportStatus_SourceUnavailable", typeof(Resources))]
        SourceUnavailable = 1,

        /// <summary>
        /// No default repository location has been configured.
        /// </summary>
        [LocalizedDescription("DicomImportStatus_DefaultRepositoryMissing", typeof(Resources))]
        DefaultRepositoryMissing = 2,

        /// <summary>
        /// The configured default repository is not available.
        /// </summary>
        [LocalizedDescription("DicomImportStatus_RepositoryUnavailable", typeof(Resources))]
        RepositoryUnavailable = 3,

        /// <summary>
        /// No valid persistent audit identity is available for the current user.
        /// </summary>
        [LocalizedDescription("DicomImportStatus_AuditIdentityUnavailable", typeof(Resources))]
        AuditIdentityUnavailable = 4,

        /// <summary>
        /// The import operation was canceled safely.
        /// </summary>
        [LocalizedDescription("DicomImportStatus_Canceled", typeof(Resources))]
        Canceled = 5,

        /// <summary>
        /// The import operation failed because of a technical error.
        /// </summary>
        [LocalizedDescription("DicomImportStatus_Failed", typeof(Resources))]
        Failed = 6,

        /// <summary>
        /// All importable files were imported successfully.
        /// </summary>
        [LocalizedDescription("DicomImportStatus_Completed", typeof(Resources))]
        Completed = 7,

        /// <summary>
        /// The import completed successfully and one or more files were skipped.
        /// </summary>
        [LocalizedDescription("DicomImportStatus_CompletedWithSkippedFiles", typeof(Resources))]
        CompletedWithSkippedFiles = 8,

        /// <summary>
        /// The import completed with one or more individual file errors.
        /// </summary>
        [LocalizedDescription("DicomImportStatus_CompletedWithErrors", typeof(Resources))]
        CompletedWithErrors = 9
    }
}