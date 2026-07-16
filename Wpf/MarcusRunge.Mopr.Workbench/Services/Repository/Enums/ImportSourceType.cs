using MarcusRunge.Mopr.Workbench.Services.Repository.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Services.Repository.Enums
{
    /// <summary>
    /// Represents the type of an import source.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ImportSourceType
    {
        /// <summary>
        /// Imports DICOM data from a directory.
        /// </summary>
        [LocalizedDescription("ImportSourceType_Directory", typeof(Resources))]
        Directory,

        /// <summary>
        /// Imports DICOM data from a CD-ROM.
        /// </summary>
        [LocalizedDescription("ImportSourceType_CdRom", typeof(Resources))]
        CdRom,

        /// <summary>
        /// Imports DICOM data from a DVD.
        /// </summary>
        [LocalizedDescription("ImportSourceType_Dvd", typeof(Resources))]
        Dvd,

        /// <summary>
        /// Imports DICOM data from a USB storage device.
        /// </summary>
        [LocalizedDescription("ImportSourceType_UsbDrive", typeof(Resources))]
        UsbDrive,

        /// <summary>
        /// Imports DICOM data from an ISO disk image.
        /// </summary>
        [LocalizedDescription("ImportSourceType_IsoImage", typeof(Resources))]
        IsoImage,

        /// <summary>
        /// Imports DICOM data from a network share.
        /// </summary>
        [LocalizedDescription("ImportSourceType_NetworkShare", typeof(Resources))]
        NetworkShare,

        /// <summary>
        /// Represents an unknown or undetected import source.
        /// </summary>
        [LocalizedDescription("ImportSourceType_Unknown", typeof(Resources))]
        Unknown
    }
}