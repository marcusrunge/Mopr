namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public sealed class ImagingViewportSelectionChangedEventArgs : EventArgs
    {
        public ImagingViewportSelectionChangedEventArgs(string oldViewportId, string newViewportId)
        {
            OldViewportId = oldViewportId;
            NewViewportId = newViewportId;
        }

        public string NewViewportId { get; }
        public string OldViewportId { get; }
    }
}