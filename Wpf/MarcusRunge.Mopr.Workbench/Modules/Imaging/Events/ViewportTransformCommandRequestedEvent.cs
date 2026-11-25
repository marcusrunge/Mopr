using Prism.Events;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Events
{
    public enum ViewportTransformCommand
    {
        RotateLeft,
        RotateRight,
        ResetRotation,
        FlipHorizontal,
        FlipVertical,
        ResetFlip,
        ResetViewTransform
    }

    public sealed class ViewportTransformCommandRequestedEvent : PubSubEvent<ViewportTransformCommand>
    {
    }
}