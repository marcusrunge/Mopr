using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Imaging
{
    /// <summary>
    /// Describes one typed persistable change in the current imaging session.
    /// </summary>
    public interface IImagingSessionChange
    {
        /// <summary>
        /// Gets the stable identity of the changed entity.
        /// </summary>
        ImagingSessionChangeIdentity Identity { get; }

        /// <summary>
        /// Gets the required persistence operation.
        /// </summary>
        ImagingSessionChangeState State { get; }

        /// <summary>
        /// Gets the payload type carried by the change.
        /// </summary>
        Type DataType { get; }

        /// <summary>
        /// Gets the value that was persisted when the entity first became dirty.
        /// </summary>
        object? OriginalValue { get; }

        /// <summary>
        /// Gets the latest in-memory value of the entity.
        /// </summary>
        object? CurrentValue { get; }

        /// <summary>
        /// Gets the monotonically increasing tracker version used to acknowledge an exact saved snapshot.
        /// </summary>
        long Version { get; }
    }
}
