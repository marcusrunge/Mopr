using MarcusRunge.Mopr.Workbench.Modules.Imaging.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Models
{
    /// <summary>
    /// Describes one explicit group of viewports and the state categories enabled for synchronization.
    /// </summary>
    public sealed class ViewportSynchronizationGroup
    {
        /// <summary>
        /// Creates an immutable group snapshot with validated, ordinally unique viewport IDs.
        /// </summary>
        public ViewportSynchronizationGroup(Guid id, string name, IEnumerable<string> viewportIds, ViewportSynchronizationFlags flags)
        {
            if (id == Guid.Empty) throw new ArgumentException("The synchronization group ID must not be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("The synchronization group name must not be empty.", nameof(name));
            ArgumentNullException.ThrowIfNull(viewportIds);
            ViewportSynchronizationFlagsValidation.Validate(flags, nameof(flags));

            Id = id;
            Name = name;
            Flags = flags;
            string[] ids = viewportIds.Select(ValidateViewportId).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            ViewportIds = Array.AsReadOnly(ids);
        }

        /// <summary>
        /// Gets the stable identity of this group.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the synchronization categories enabled for this group.
        /// </summary>
        public ViewportSynchronizationFlags Flags { get; }

        /// <summary>
        /// Gets the display name of this group.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a defensive, ordinally sorted snapshot of the member viewport IDs.
        /// </summary>
        public IReadOnlyCollection<string> ViewportIds { get; }

        internal static string ValidateViewportId(string viewportId)
        {
            if (string.IsNullOrWhiteSpace(viewportId)) throw new ArgumentException("A viewport ID must not be empty.", nameof(viewportId));
            return viewportId;
        }
    }
}
