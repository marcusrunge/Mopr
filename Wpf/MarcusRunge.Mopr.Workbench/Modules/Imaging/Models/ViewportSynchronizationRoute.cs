using MarcusRunge.Mopr.Workbench.Modules.Imaging.Enums;
using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Models
{
    /// <summary>
    /// Identifies one directed synchronization relationship without carrying viewport state.
    /// </summary>
    public sealed class ViewportSynchronizationRoute
    {
        /// <summary>
        /// Creates a validated route from one source viewport to a distinct target viewport.
        /// </summary>
        public ViewportSynchronizationRoute(Guid groupId, string sourceViewportId, string targetViewportId, ViewportSynchronizationFlags synchronizationType)
        {
            if (groupId == Guid.Empty) throw new ArgumentException("The synchronization group ID must not be empty.", nameof(groupId));
            ViewportSynchronizationGroup.ValidateViewportId(sourceViewportId);
            ViewportSynchronizationGroup.ValidateViewportId(targetViewportId);
            if (StringComparer.Ordinal.Equals(sourceViewportId, targetViewportId)) throw new ArgumentException("A synchronization route must have distinct source and target viewports.", nameof(targetViewportId));
            ViewportSynchronizationFlagsValidation.ValidateSingle(synchronizationType, nameof(synchronizationType));

            GroupId = groupId;
            SourceViewportId = sourceViewportId;
            TargetViewportId = targetViewportId;
            SynchronizationType = synchronizationType;
        }

        /// <summary>
        /// Gets the group that produced this route.
        /// </summary>
        public Guid GroupId { get; }

        /// <summary>
        /// Gets the source viewport ID.
        /// </summary>
        public string SourceViewportId { get; }

        /// <summary>
        /// Gets the synchronization category for this route.
        /// </summary>
        public ViewportSynchronizationFlags SynchronizationType { get; }

        /// <summary>
        /// Gets the target viewport ID.
        /// </summary>
        public string TargetViewportId { get; }
    }
}
