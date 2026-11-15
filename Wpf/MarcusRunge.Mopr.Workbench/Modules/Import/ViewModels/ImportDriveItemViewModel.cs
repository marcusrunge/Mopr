using MarcusRunge.Mopr.Workbench.Core.Mvvm;
using MarcusRunge.Mopr.Workbench.Modules.Import.Properties;
using MarcusRunge.Mopr.Workbench.Services.Application.Models.Import;
using System.IO;

namespace MarcusRunge.Mopr.Workbench.Modules.Import.ViewModels
{
    /// <summary>
    /// Represents an operating-system drive available for DICOM import-source selection.
    /// </summary>
    public sealed class ImportDriveItemViewModel : ViewModelBase
    {
        public ImportDriveItemViewModel(DicomImportDriveInfo drive)
        {
            ArgumentNullException.ThrowIfNull(drive);

            RootPath = drive.RootPath;
            DriveType = drive.DriveType;
            IsReady = drive.IsReady;
            DisplayName = CreateDisplayName(drive);
            StatusText = drive.IsReady ? Resources.ImportDriveStatusReady : Resources.ImportDriveStatusNotReady;
        }

        public string DisplayName { get; }
        public DriveType DriveType { get; }
        public bool IsReady { get; }
        public string RootPath { get; }
        public string StatusText { get; }

        private static string CreateDisplayName(DicomImportDriveInfo drive)
        {
            var sourceName = drive.DriveType switch
            {
                DriveType.CDRom => Resources.ImportDriveTypeOpticalMedia,
                DriveType.Network => Resources.ImportDriveTypeNetworkDrive,
                DriveType.Ram => Resources.ImportDriveTypeVirtualDrive,
                DriveType.Removable => Resources.ImportDriveTypeRemovableMedia,
                DriveType.Fixed => Resources.ImportDriveTypeLocalDrive,
                _ => Resources.ImportDriveTypeOther
            };

            return $"{sourceName} ({drive.RootPath})";
        }
    }
}