using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Enums
{
    /// <summary>
    /// Represents the orientation of the viewport.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ViewportOrientation
    {
        /// <summary>
        /// Represents an unknown viewport orientation.
        /// </summary>
        [LocalizedDescription("ViewportOrientation_Unknown", typeof(Resources))]
        Unknown = 0,
        /// <summary>
        /// Represents an axial viewport orientation.
        /// </summary>
        [LocalizedDescription("ViewportOrientation_Axial", typeof(Resources))]
        Axial = 1,
        /// <summary>
        /// Represents a sagittal viewport orientation.
        /// </summary>
        [LocalizedDescription("ViewportOrientation_Sagittal", typeof(Resources))]
        Sagittal = 2,
        /// <summary>
        /// Represents a coronal viewport orientation.
        /// </summary>
        [LocalizedDescription("ViewportOrientation_Coronal", typeof(Resources))]
        Coronal = 3,
        /// <summary>
        /// Represents a volume preview viewport orientation.
        /// </summary>
        [LocalizedDescription("ViewportOrientation_VolumePreview", typeof(Resources))]
        VolumePreview = 4,
        /// <summary>
        /// Represents a generic viewport orientation.
        /// </summary>
        [LocalizedDescription("ViewportOrientation_Generic", typeof(Resources))]
        Generic = 5
    }
}