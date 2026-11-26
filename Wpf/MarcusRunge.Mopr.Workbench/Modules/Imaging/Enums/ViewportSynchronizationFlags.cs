using MarcusRunge.Mopr.Workbench.Modules.Imaging.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System;
using System.ComponentModel;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Enums
{
    /// <summary>
    /// Identifies the viewport state categories that may be synchronized explicitly.
    /// </summary>
    [Flags]
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ViewportSynchronizationFlags
    {
        /// <summary>
        /// Disables synchronization.
        /// </summary>
        [LocalizedDescription("ViewportSynchronization_None", typeof(Resources))]
        None = 0,

        /// <summary>
        /// Synchronizes compatible slice positions.
        /// </summary>
        [LocalizedDescription("ViewportSynchronization_Slice", typeof(Resources))]
        Slice = 1,

        /// <summary>
        /// Synchronizes window and level values.
        /// </summary>
        [LocalizedDescription("ViewportSynchronization_WindowLevel", typeof(Resources))]
        WindowLevel = 2,

        /// <summary>
        /// Synchronizes zoom values.
        /// </summary>
        [LocalizedDescription("ViewportSynchronization_Zoom", typeof(Resources))]
        Zoom = 4,

        /// <summary>
        /// Synchronizes pan offsets.
        /// </summary>
        [LocalizedDescription("ViewportSynchronization_Pan", typeof(Resources))]
        Pan = 8,

        /// <summary>
        /// Synchronizes image rotation.
        /// </summary>
        [LocalizedDescription("ViewportSynchronization_Rotation", typeof(Resources))]
        Rotation = 16,

        /// <summary>
        /// Synchronizes horizontal image flipping.
        /// </summary>
        [LocalizedDescription("ViewportSynchronization_HorizontalFlip", typeof(Resources))]
        HorizontalFlip = 32,

        /// <summary>
        /// Synchronizes vertical image flipping.
        /// </summary>
        [LocalizedDescription("ViewportSynchronization_VerticalFlip", typeof(Resources))]
        VerticalFlip = 64
    }

    internal static class ViewportSynchronizationFlagsValidation
    {
        internal const ViewportSynchronizationFlags Supported = ViewportSynchronizationFlags.Slice | ViewportSynchronizationFlags.WindowLevel | ViewportSynchronizationFlags.Zoom | ViewportSynchronizationFlags.Pan | ViewportSynchronizationFlags.Rotation | ViewportSynchronizationFlags.HorizontalFlip | ViewportSynchronizationFlags.VerticalFlip;

        internal static bool IsValid(ViewportSynchronizationFlags flags) => (flags & ~Supported) == 0;

        internal static bool IsSingleSupported(ViewportSynchronizationFlags flag)
        {
            var value = (int)flag;
            return value > 0 && IsValid(flag) && (value & (value - 1)) == 0;
        }

        internal static void Validate(ViewportSynchronizationFlags flags, string parameterName)
        {
            if (!IsValid(flags)) throw new ArgumentOutOfRangeException(parameterName, flags, "The synchronization flags contain unsupported bits.");
        }

        internal static void ValidateSingle(ViewportSynchronizationFlags flag, string parameterName)
        {
            if (!IsSingleSupported(flag)) throw new ArgumentOutOfRangeException(parameterName, flag, "Exactly one supported synchronization flag is required.");
        }
    }
}