using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Implementations.Imaging
{
    /// <summary>
    /// Maintains the authoritative set of unsaved persistable imaging changes for the current session.
    /// </summary>
    internal sealed class ImagingSessionChangeTracker : CreateableBindableBase<IImagingSessionChangeTracker, ImagingSessionChangeTracker, IImagingServiceBase>, IImagingSessionChangeTracker
    {
        private readonly object _syncRoot = new();
        private readonly Dictionary<string, ImagingUnsavedChange> _unsavedChanges = new(StringComparer.Ordinal);

        public event EventHandler? UnsavedChangesChanged;

        public bool HasUnsavedChanges
        {
            get
            {
                lock (_syncRoot) return _unsavedChanges.Count > 0;
            }
        }

        public IReadOnlyList<ImagingUnsavedChange> UnsavedChanges
        {
            get
            {
                lock (_syncRoot) return [.. _unsavedChanges.Values];
            }
        }

        public void Clear()
        {
            bool changed;

            lock (_syncRoot)
            {
                changed = _unsavedChanges.Count > 0;
                _unsavedChanges.Clear();
            }

            if (changed) UnsavedChangesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MarkUnsaved(string owner, string key, ImagingUnsavedChange change)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("The change owner must not be empty.", nameof(owner));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("The change key must not be empty.", nameof(key));
            if (change is null) throw new ArgumentNullException(nameof(change));

            var compositeKey = CreateCompositeKey(owner, key);

            lock (_syncRoot) _unsavedChanges[compositeKey] = change;

            UnsavedChangesChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool MarkSaved(string owner, string key)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("The change owner must not be empty.", nameof(owner));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("The change key must not be empty.", nameof(key));

            bool removed;

            lock (_syncRoot) removed = _unsavedChanges.Remove(CreateCompositeKey(owner, key));

            if (removed) UnsavedChangesChanged?.Invoke(this, EventArgs.Empty);

            return removed;
        }

        protected override void OnCreate(IImagingServiceBase @base)
        {
        }

        protected override Task OnCreateAsync(IImagingServiceBase @base, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        private static string CreateCompositeKey(string owner, string key) => $"{owner.Trim()}\u001F{key.Trim()}";
    }
}