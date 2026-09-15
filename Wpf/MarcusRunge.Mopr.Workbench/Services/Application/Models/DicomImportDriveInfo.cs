using System.IO;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Contracts.Import
{
    /// <summary>
    /// Contains the drive information required for import-source classification.
    /// </summary>
    internal sealed record DicomImportDriveInfo
    {
        /// <summary>
        /// Initializes a drive descriptor.
        /// </summary>
        /// <param name="rootPath">The drive root path.</param>
        /// <param name="driveType">The operating-system drive type.</param>
        /// <param name="isReady">Indicates whether the drive currently contains an accessible medium.</param>
        internal DicomImportDriveInfo(string rootPath, DriveType driveType, bool isReady)
        {
            RootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
            DriveType = driveType;
            IsReady = isReady;
        }

        /// <summary>
        /// Gets the operating-system drive type.
        /// </summary>
        internal DriveType DriveType { get; }

        /// <summary>
        /// Gets a value indicating whether the drive currently contains an accessible medium.
        /// </summary>
        internal bool IsReady { get; }

        /// <summary>
        /// Gets the drive root path.
        /// </summary>
        internal string RootPath { get; }
    }
}