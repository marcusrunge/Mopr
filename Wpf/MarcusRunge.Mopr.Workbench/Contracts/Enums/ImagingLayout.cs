using MarcusRunge.Mopr.Workbench.Contracts.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Contracts.Enums
{
    /// <summary>
    /// Represents the layout of the imaging view.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ImagingLayout
    {
        /// <summary>
        /// Represents a single imaging view.
        /// </summary>
        [LocalizedDescription("Viewer_Layout_Single", typeof(Resources))]
        Single = 0,
        /// <summary>
        /// Represents a 2x2 grid of imaging views.
        /// </summary>
        [LocalizedDescription("Viewer_Layout_TwoByTwo", typeof(Resources))]
        TwoByTwo = 1,
        /// <summary>
        /// Represents a multi-planar reconstruction layout.
        /// </summary>
        [LocalizedDescription("Viewer_Layout_Mpr", typeof(Resources))]
        Mpr = 2,
        /// <summary>
        /// Represents an axial, sagittal, and coronal layout.
        /// </summary>
        [LocalizedDescription("Viewer_Layout_AxialSagittalCoronal", typeof(Resources))]
        AxialSagittalCoronal = 3,
        /// <summary>
        /// Represents an unknown layout.
        /// </summary>
        [LocalizedDescription("Viewer_Layout_Unknown", typeof(Resources))]
        Unknown = 4
    }
}