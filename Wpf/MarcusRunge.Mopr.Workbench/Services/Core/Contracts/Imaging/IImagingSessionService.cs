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
        /// Clears the current study and every transient imaging state after the caller has resolved unsaved changes.
        /// </summary>
        void Clear();
    }
}