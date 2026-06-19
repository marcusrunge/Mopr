using MarcusRunge.Mopr.Workbench.Contracts.Imaging;

namespace MarcusRunge.Mopr.Workbench.Contracts.Models
{
    public sealed class ViewportDescriptor
    {
        public ViewportDescriptor(string id, string title, ViewportOrientation orientation, bool isInteractive = true)
        {
            Id = id;
            Title = title;
            Orientation = orientation;
            IsInteractive = isInteractive;
        }

        public string Id { get; }

        public bool IsInteractive { get; }
        public ViewportOrientation Orientation { get; }
        public string Title { get; }
    }
}