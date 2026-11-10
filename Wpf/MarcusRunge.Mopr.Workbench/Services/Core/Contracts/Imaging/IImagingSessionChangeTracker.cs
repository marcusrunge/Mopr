using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using System;
using System.Collections.Generic;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    /// <summary>
    /// Tracks persistable imaging changes that must be protected before a study is closed or replaced.
    /// </summary>
    public interface IImagingSessionChangeTracker
    {
        /// <summary>
        /// Occurs when the unsaved-change state changes.
        /// </summary>
        event EventHandler? UnsavedChangesChanged;

        /// <summary>
        /// Gets a value indicating whether persistable imaging changes are pending.
        /// </summary>
        bool HasUnsavedChanges { get; }

        /// <summary>
        /// Gets a snapshot of all currently registered unsaved changes.
        /// </summary>
        IReadOnlyList<ImagingUnsavedChange> UnsavedChanges { get; }

        /// <summary>
        /// Clears every registered change after a successful save or an explicit discard operation.
        /// </summary>
        void Clear();

        /// <summary>
        /// Registers or replaces one change under a stable owner and change key.
        /// </summary>
        void MarkUnsaved(string owner, string key, ImagingUnsavedChange change);

        /// <summary>
        /// Removes one registered change after that change has been saved or reverted.
        /// </summary>
        bool MarkSaved(string owner, string key);
    }
}