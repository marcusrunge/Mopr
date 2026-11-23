using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;
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
    /// Maintains the authoritative typed change set for the current imaging session.
    /// </summary>
    internal sealed class ImagingSessionChangeTracker : CreateableBindableBase<IImagingSessionChangeTracker, ImagingSessionChangeTracker, IImagingServiceBase>, IImagingSessionChangeTracker
    {
        private readonly object _syncRoot = new();
        private readonly Dictionary<ImagingSessionChangeIdentity, IImagingSessionChange> _unsavedChanges = [];
        private long _version;

        public event EventHandler? UnsavedChangesChanged;

        public bool HasUnsavedChanges
        {
            get
            {
                lock (_syncRoot) return _unsavedChanges.Count > 0;
            }
        }

        public IReadOnlyList<IImagingSessionChange> UnsavedChanges
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

        public void MarkChanged<TData>(ImagingSessionChangeIdentity identity, ImagingSessionChangeState state, TData? originalValue, TData? currentValue)
        {
            if (identity is null) throw new ArgumentNullException(nameof(identity));

            bool changed;

            lock (_syncRoot)
            {
                _unsavedChanges.TryGetValue(identity, out var existingChange);
                var mergedChange = Merge(existingChange, identity, state, originalValue, currentValue, NextVersion());
                changed = ApplyMergedChange(identity, existingChange, mergedChange);
            }

            if (changed) UnsavedChangesChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool MarkSaved(ImagingSessionChangeIdentity identity, long version)
        {
            if (identity is null) throw new ArgumentNullException(nameof(identity));
            if (version <= 0) throw new ArgumentOutOfRangeException(nameof(version), "The saved change version must be positive.");

            bool removed;

            lock (_syncRoot) removed = _unsavedChanges.TryGetValue(identity, out var change) && change.Version == version && _unsavedChanges.Remove(identity);

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

        private bool ApplyMergedChange(ImagingSessionChangeIdentity identity, IImagingSessionChange? existingChange, IImagingSessionChange? mergedChange)
        {
            if (mergedChange is null) return existingChange is not null && _unsavedChanges.Remove(identity);
            if (Equals(existingChange, mergedChange)) return false;

            _unsavedChanges[identity] = mergedChange;
            return true;
        }

        private static IImagingSessionChange? Merge<TData>(IImagingSessionChange? existingChange, ImagingSessionChangeIdentity identity, ImagingSessionChangeState state, TData? originalValue, TData? currentValue, long version)
        {
            if (existingChange is null) return new ImagingSessionChange<TData>(identity, state, originalValue, currentValue, version);
            if (existingChange is not ImagingSessionChange<TData> typedChange) throw new InvalidOperationException($"The entity '{identity.EntityType}:{identity.EntityId}' was already tracked with payload type '{existingChange.DataType.FullName}' and cannot be changed to '{typeof(TData).FullName}'.");

            return (typedChange.State, state) switch
            {
                (ImagingSessionChangeState.Created, ImagingSessionChangeState.Created) => new ImagingSessionChange<TData>(identity, ImagingSessionChangeState.Created, default, currentValue, version),
                (ImagingSessionChangeState.Created, ImagingSessionChangeState.Modified) => new ImagingSessionChange<TData>(identity, ImagingSessionChangeState.Created, default, currentValue, version),
                (ImagingSessionChangeState.Created, ImagingSessionChangeState.Deleted) => null,
                (ImagingSessionChangeState.Modified, ImagingSessionChangeState.Modified) => new ImagingSessionChange<TData>(identity, ImagingSessionChangeState.Modified, typedChange.OriginalValue, currentValue, version),
                (ImagingSessionChangeState.Modified, ImagingSessionChangeState.Deleted) => new ImagingSessionChange<TData>(identity, ImagingSessionChangeState.Deleted, typedChange.OriginalValue, default, version),
                (ImagingSessionChangeState.Deleted, ImagingSessionChangeState.Created) => new ImagingSessionChange<TData>(identity, ImagingSessionChangeState.Modified, typedChange.OriginalValue, currentValue, version),
                _ => throw new InvalidOperationException($"The transition from '{typedChange.State}' to '{state}' is not valid for '{identity.EntityType}:{identity.EntityId}'.")
            };
        }

        private long NextVersion() => checked(++_version);
    }
}