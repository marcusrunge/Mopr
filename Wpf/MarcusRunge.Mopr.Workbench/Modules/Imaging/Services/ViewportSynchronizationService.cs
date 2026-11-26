using MarcusRunge.Mopr.Workbench.Modules.Imaging.Enums;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    /// <summary>
    /// Stores explicit viewport synchronization groups and resolves immutable routing snapshots.
    /// </summary>
    public sealed class ViewportSynchronizationService : IViewportSynchronizationService
    {
        // Replacing immutable groups under one gate keeps each membership update atomic to concurrent readers.
        private readonly object _gate = new();
        private readonly Dictionary<Guid, ViewportSynchronizationGroup> _groups = [];

        /// <inheritdoc/>
        public ViewportSynchronizationGroup AddViewport(Guid groupId, string viewportId)
        {
            ViewportSynchronizationGroup.ValidateViewportId(viewportId);
            lock (_gate)
            {
                ViewportSynchronizationGroup group = GetRequiredGroup(groupId);
                if (group.ViewportIds.Contains(viewportId, StringComparer.Ordinal)) return group;
                ViewportSynchronizationGroup updated = new(group.Id, group.Name, group.ViewportIds.Append(viewportId), group.Flags);
                _groups[groupId] = updated;
                return updated;
            }
        }

        /// <inheritdoc/>
        public ViewportSynchronizationGroup CreateGroup(string name, IEnumerable<string> viewportIds, ViewportSynchronizationFlags flags = ViewportSynchronizationFlags.None)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The synchronization group name must not be empty.", nameof(name));
            ArgumentNullException.ThrowIfNull(viewportIds);
            ViewportSynchronizationFlagsValidation.Validate(flags, nameof(flags));
            var group = new ViewportSynchronizationGroup(Guid.NewGuid(), name, viewportIds, flags);
            lock (_gate)
            {
                _groups.Add(group.Id, group);
                return group;
            }
        }

        /// <inheritdoc/>
        public ViewportSynchronizationGroup? GetGroup(Guid groupId)
        {
            ValidateGroupId(groupId);
            lock (_gate) return _groups.GetValueOrDefault(groupId);
        }

        /// <inheritdoc/>
        public IReadOnlyList<ViewportSynchronizationGroup> GetGroups()
        {
            // Callers enumerate snapshots after the lock is released, never the mutable backing collection.
            lock (_gate) return Array.AsReadOnly(_groups.Values.OrderBy(group => group.Id).ToArray());
        }

        /// <inheritdoc/>
        public IReadOnlyList<ViewportSynchronizationGroup> GetGroupsForViewport(string viewportId)
        {
            ViewportSynchronizationGroup.ValidateViewportId(viewportId);
            lock (_gate) return Array.AsReadOnly(_groups.Values.Where(group => group.ViewportIds.Contains(viewportId, StringComparer.Ordinal)).OrderBy(group => group.Id).ToArray());
        }

        /// <inheritdoc/>
        public IReadOnlyList<ViewportSynchronizationRoute> GetRoutes(string sourceViewportId, ViewportSynchronizationFlags synchronizationType)
        {
            ViewportSynchronizationGroup.ValidateViewportId(sourceViewportId);
            ViewportSynchronizationFlagsValidation.Validate(synchronizationType, nameof(synchronizationType));
            if (synchronizationType == ViewportSynchronizationFlags.None) return Array.AsReadOnly(Array.Empty<ViewportSynchronizationRoute>());
            ViewportSynchronizationFlagsValidation.ValidateSingle(synchronizationType, nameof(synchronizationType));

            lock (_gate)
            {
                var targetGroups = new Dictionary<string, Guid>(StringComparer.Ordinal);
                foreach (ViewportSynchronizationGroup group in _groups.Values.OrderBy(group => group.Id))
                {
                    if (group.ViewportIds.Count < 2 || (group.Flags & synchronizationType) == 0 || !group.ViewportIds.Contains(sourceViewportId, StringComparer.Ordinal)) continue;
                    foreach (string targetViewportId in group.ViewportIds)
                    {
                        if (!StringComparer.Ordinal.Equals(sourceViewportId, targetViewportId)) targetGroups.TryAdd(targetViewportId, group.Id);
                    }
                }

                ViewportSynchronizationRoute[] routes = targetGroups.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new ViewportSynchronizationRoute(pair.Value, sourceViewportId, pair.Key, synchronizationType))
                    .ToArray();
                // Returning a read-only snapshot keeps callers from mutating routing state after the lock is released.
                return Array.AsReadOnly(routes);
            }
        }

        /// <inheritdoc/>
        public bool RemoveGroup(Guid groupId)
        {
            ValidateGroupId(groupId);
            lock (_gate) return _groups.Remove(groupId);
        }

        /// <inheritdoc/>
        public ViewportSynchronizationGroup RemoveViewport(Guid groupId, string viewportId)
        {
            ViewportSynchronizationGroup.ValidateViewportId(viewportId);
            lock (_gate)
            {
                ViewportSynchronizationGroup group = GetRequiredGroup(groupId);
                if (!group.ViewportIds.Contains(viewportId, StringComparer.Ordinal)) return group;
                ViewportSynchronizationGroup updated = new(group.Id, group.Name, group.ViewportIds.Where(id => !StringComparer.Ordinal.Equals(id, viewportId)), group.Flags);
                _groups[groupId] = updated;
                return updated;
            }
        }

        /// <inheritdoc/>
        public ViewportSynchronizationGroup RenameGroup(Guid groupId, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The synchronization group name must not be empty.", nameof(name));
            lock (_gate)
            {
                ViewportSynchronizationGroup group = GetRequiredGroup(groupId);
                ViewportSynchronizationGroup updated = new(group.Id, name, group.ViewportIds, group.Flags);
                _groups[groupId] = updated;
                return updated;
            }
        }

        /// <inheritdoc/>
        public ViewportSynchronizationGroup SetGroupFlags(Guid groupId, ViewportSynchronizationFlags flags)
        {
            ViewportSynchronizationFlagsValidation.Validate(flags, nameof(flags));
            lock (_gate)
            {
                ViewportSynchronizationGroup group = GetRequiredGroup(groupId);
                ViewportSynchronizationGroup updated = new(group.Id, group.Name, group.ViewportIds, flags);
                _groups[groupId] = updated;
                return updated;
            }
        }

        private ViewportSynchronizationGroup GetRequiredGroup(Guid groupId)
        {
            ValidateGroupId(groupId);
            return _groups.TryGetValue(groupId, out ViewportSynchronizationGroup? group)
                ? group
                : throw new KeyNotFoundException($"Synchronization group '{groupId}' does not exist.");
        }

        private static void ValidateGroupId(Guid groupId)
        {
            if (groupId == Guid.Empty) throw new ArgumentException("The synchronization group ID must not be empty.", nameof(groupId));
        }
    }
}
