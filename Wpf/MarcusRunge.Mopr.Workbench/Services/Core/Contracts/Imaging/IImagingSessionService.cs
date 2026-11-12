using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    /// <summary>
    /// Coordinates the lifecycle of the currently open imaging study.
    /// </summary>
    public interface IImagingSessionService
    {
        /// <summary>
        /// Occurs when the open-study or unsaved-change state changes.
        /// </summary>
        event EventHandler? StateChanged;

        /// <summary>
        /// Gets a value indicating whether an imaging study is currently open.
        /// </summary>
        bool HasOpenStudy { get; }

        /// <summary>
        /// Gets a value indicating whether persistable imaging changes are pending.
        /// </summary>
        bool HasUnsavedChanges { get; }

        /// <summary>
        /// Clears a clean current study and every transient imaging state.
        /// </summary>
        void Clear();

        /// <summary>
        /// Explicitly discards every unsaved change and clears the current study and transient imaging state.
        /// </summary>
        void DiscardChangesAndClear();
    }
}