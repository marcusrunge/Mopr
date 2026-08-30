using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public sealed class ImagingViewportSelectionChangedEventArgs(string oldViewportId, string newViewportId) : EventArgs
    {
        public string NewViewportId { get; } = newViewportId;
        public string OldViewportId { get; } = oldViewportId;
    }
}