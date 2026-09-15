using MarcusRunge.Mopr.Workbench.Services.Application.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Services.Application.Enums
{
    /// <summary>
    /// Identifies the user-facing source of a DICOM import operation.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum DicomImportSourceType
    {
        /// <summary>
        /// Detects the source type from the selected path and the available operating-system information.
        /// </summary>
        [LocalizedDescription("DicomImportSourceType_AutoDetect", typeof(Resources))]
        AutoDetect = 0,

        /// <summary>
        /// Imports DICOM data from a directory on a local fixed drive.
        /// </summary>
        [LocalizedDescription("DicomImportSourceType_LocalDirectory", typeof(Resources))]
        LocalDirectory = 1,

        /// <summary>
        /// Imports DICOM data from a USB storage device.
        /// </summary>
        [LocalizedDescription("DicomImportSourceType_UsbDrive", typeof(Resources))]
        UsbDrive = 2,

        /// <summary>
        /// Imports DICOM data from an operating-system classified removable drive.
        /// </summary>
        [LocalizedDescription("DicomImportSourceType_RemovableDrive", typeof(Resources))]
        RemovableDrive = 3,

        /// <summary>
        /// Imports DICOM data from an external fixed drive.
        /// </summary>
        [LocalizedDescription("DicomImportSourceType_ExternalDrive", typeof(Resources))]
        ExternalDrive = 4,

        /// <summary>
        /// Imports DICOM data from an SD or equivalent memory card.
        /// </summary>
        [LocalizedDescription("DicomImportSourceType_SdCard", typeof(Resources))]
        SdCard = 5,

        /// <summary>
        /// Imports DICOM data from a CD-ROM.
        /// </summary>
        [LocalizedDescription("DicomImportSourceType_CdRom", typeof(Resources))]
        CdRom = 6,

        /// <summary>
        /// Imports DICOM data from a DVD.
        /// </summary>
        [LocalizedDescription("DicomImportSourceType_Dvd", typeof(Resources))]
        Dvd = 7,

        /// <summary>
        /// Imports DICOM data from a UNC network share.
        /// </summary>
        [LocalizedDescription("DicomImportSourceType_NetworkShare", typeof(Resources))]
        NetworkShare = 8,

        /// <summary>
        /// Imports DICOM data from an operating-system mapped network drive.
        /// </summary>
        [LocalizedDescription("DicomImportSourceType_MappedNetworkDrive", typeof(Resources))]
        MappedNetworkDrive = 9,

        /// <summary>
        /// Imports DICOM data from an ISO disk-image file.
        /// </summary>
        [LocalizedDescription("DicomImportSourceType_IsoImage", typeof(Resources))]
        IsoImage = 10,

        /// <summary>
        /// Imports DICOM data from an already mounted virtual drive.
        /// </summary>
        [LocalizedDescription("DicomImportSourceType_VirtualDrive", typeof(Resources))]
        VirtualDrive = 11,

        /// <summary>
        /// Represents a source that could not be classified safely.
        /// </summary>
        [LocalizedDescription("DicomImportSourceType_Unknown", typeof(Resources))]
        Unknown = 12
    }
}