using MarcusRunge.Mopr.Workbench.Modules.Imaging.Enums;
using MarcusRunge.Mopr.Workbench.Modules.Imaging.Models;
using System;
using System.Collections.Generic;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Services
{
    /// <summary>
    /// Manages explicit viewport synchronization groups and resolves directed routes between their members.
    /// </summary>
    public interface IViewportSynchronizationService
    {
        /// <summary>
        /// Adds one viewport to a group, returning the updated immutable group snapshot.
        /// </summary>
        ViewportSynchronizationGroup AddViewport(Guid groupId, string viewportId);

        /// <summary>
        /// Creates a group with a generated stable ID and no implicit synchronization categories.
        /// </summary>
        ViewportSynchronizationGroup CreateGroup(string name, IEnumerable<string> viewportIds, ViewportSynchronizationFlags flags = ViewportSynchronizationFlags.None);

        /// <summary>
        /// Removes a group and returns whether it existed.
        /// </summary>
        bool RemoveGroup(Guid groupId);

        /// <summary>
        /// Removes one viewport from a group, returning the updated immutable group snapshot.
        /// </summary>
        ViewportSynchronizationGroup RemoveViewport(Guid groupId, string viewportId);

        /// <summary>
        /// Renames a group without changing its identity or members.
        /// </summary>
        ViewportSynchronizationGroup RenameGroup(Guid groupId, string name);

        /// <summary>
        /// Replaces all synchronization categories enabled for a group.
        /// </summary>
        ViewportSynchronizationGroup SetGroupFlags(Guid groupId, ViewportSynchronizationFlags flags);

        /// <summary>
        /// Gets an immutable snapshot of a group, or null when the ID is unknown.
        /// </summary>
        ViewportSynchronizationGroup? GetGroup(Guid groupId);

        /// <summary>
        /// Gets an immutable snapshot of all groups in deterministic ID order.
        /// </summary>
        IReadOnlyList<ViewportSynchronizationGroup> GetGroups();

        /// <summary>
        /// Gets an immutable snapshot of all groups containing the specified viewport.
        /// </summary>
        IReadOnlyList<ViewportSynchronizationGroup> GetGroupsForViewport(string viewportId);

        /// <summary>
        /// Resolves deterministic, deduplicated routes for one supported synchronization category.
        /// </summary>
        IReadOnlyList<ViewportSynchronizationRoute> GetRoutes(string sourceViewportId, ViewportSynchronizationFlags synchronizationType);
    }
}
