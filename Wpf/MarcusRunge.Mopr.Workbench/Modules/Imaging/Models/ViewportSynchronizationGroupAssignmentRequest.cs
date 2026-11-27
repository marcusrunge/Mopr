using System;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Models
{
    public sealed record ViewportSynchronizationGroupAssignmentRequest(string ViewportId, Guid GroupId)
    {
        public string ViewportId { get; } = !string.IsNullOrWhiteSpace(ViewportId) ? ViewportId : throw new ArgumentException("The viewport ID must not be empty.", nameof(ViewportId));
        public Guid GroupId { get; } = GroupId != Guid.Empty ? GroupId : throw new ArgumentException("The group ID must not be empty.", nameof(GroupId));
    }
}