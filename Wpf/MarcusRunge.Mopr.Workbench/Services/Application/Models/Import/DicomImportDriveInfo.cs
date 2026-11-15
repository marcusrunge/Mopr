using System.IO;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Models.Import
{
    /// <summary>
    /// Contains the drive information required for import-source selection and classification.
    /// </summary>
    public sealed record DicomImportDriveInfo
    {
        /// <summary>
        /// Initializes a drive descriptor.
        /// </summary>
        /// <param name="rootPath">The drive root path.</param>
        /// <param name="driveType">The operating-system drive type.</param>
        /// <param name="isReady">Indicates whether the drive currently contains an accessible medium.</param>
        public DicomImportDriveInfo(string rootPath, DriveType driveType, bool isReady)
        {
            RootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
            DriveType = driveType;
            IsReady = isReady;
        }

        /// <summary>
        /// Gets the operating-system drive type.
        /// </summary>
        public DriveType DriveType { get; }

        /// <summary>
        /// Gets a value indicating whether the drive currently contains an accessible medium.
        /// </summary>
        public bool IsReady { get; }

        /// <summary>
        /// Gets the drive root path.
        /// </summary>
        public string RootPath { get; }
    }
}