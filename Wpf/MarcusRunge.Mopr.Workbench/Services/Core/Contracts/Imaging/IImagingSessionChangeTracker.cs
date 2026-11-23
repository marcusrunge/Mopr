using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using System;
using System.Collections.Generic;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    /// <summary>
    /// Tracks typed persistable changes that belong to the currently open imaging study.
    /// </summary>
    public interface IImagingSessionChangeTracker
    {
        /// <summary>
        /// Occurs when the unsaved-change snapshot changes.
        /// </summary>
        event EventHandler? UnsavedChangesChanged;

        /// <summary>
        /// Gets a value indicating whether persistable imaging changes are pending.
        /// </summary>
        bool HasUnsavedChanges { get; }

        /// <summary>
        /// Gets a stable snapshot of all currently registered unsaved changes.
        /// </summary>
        IReadOnlyList<IImagingSessionChange> UnsavedChanges { get; }

        /// <summary>
        /// Clears every registered change after an explicit discard operation.
        /// </summary>
        void Clear();

        /// <summary>
        /// Registers and merges one typed change under its stable entity identity.
        /// </summary>
        void MarkChanged<TData>(ImagingSessionChangeIdentity identity, ImagingSessionChangeState state, TData? originalValue, TData? currentValue);

        /// <summary>
        /// Removes a change only when its identity and version still match the successfully saved snapshot.
        /// </summary>
        bool MarkSaved(ImagingSessionChangeIdentity identity, long version);
    }
}