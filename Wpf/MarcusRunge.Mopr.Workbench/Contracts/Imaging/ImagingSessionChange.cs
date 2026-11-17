using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using System;

namespace MarcusRunge.Mopr.Workbench.Contracts.Imaging
{
    /// <summary>
    /// Describes one typed persistable change in the current imaging session.
    /// </summary>
    public sealed record ImagingSessionChange<TData> : IImagingSessionChange
    {
        /// <summary>
        /// Initializes a new typed imaging-session change.
        /// </summary>
        public ImagingSessionChange(ImagingSessionChangeIdentity identity, ImagingSessionChangeState state, TData? originalValue, TData? currentValue, long version)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            if (version <= 0) throw new ArgumentOutOfRangeException(nameof(version), "The change version must be positive.");
            if (state == ImagingSessionChangeState.Created && currentValue is null) throw new ArgumentNullException(nameof(currentValue), "A created change requires a current value.");
            if (state == ImagingSessionChangeState.Modified && (originalValue is null || currentValue is null)) throw new ArgumentException("A modified change requires original and current values.");
            if (state == ImagingSessionChangeState.Deleted && originalValue is null) throw new ArgumentNullException(nameof(originalValue), "A deleted change requires an original value.");

            State = state;
            OriginalValue = originalValue;
            CurrentValue = currentValue;
            Version = version;
        }

        /// <inheritdoc/>
        public ImagingSessionChangeIdentity Identity { get; }

        /// <inheritdoc/>
        public ImagingSessionChangeState State { get; }

        /// <inheritdoc/>
        public TData? OriginalValue { get; }

        /// <inheritdoc/>
        public TData? CurrentValue { get; }

        /// <inheritdoc/>
        public long Version { get; }

        Type IImagingSessionChange.DataType => typeof(TData);

        object? IImagingSessionChange.OriginalValue => OriginalValue;

        object? IImagingSessionChange.CurrentValue => CurrentValue;
    }
}
