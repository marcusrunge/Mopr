using MarcusRunge.Mopr.Workbench.Contracts.Enums;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models
{
    public sealed class ViewportDescriptor(string id, string title, ViewportOrientation orientation, bool isInteractive = true)
    {
        public string Id { get; } = id;
        public bool IsInteractive { get; } = isInteractive;
        public ViewportOrientation Orientation { get; } = orientation;
        public string Title { get; } = title;
    }
}