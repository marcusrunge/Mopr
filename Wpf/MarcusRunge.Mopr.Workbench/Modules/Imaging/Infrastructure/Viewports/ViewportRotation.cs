namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports
{
    /// <summary>Defines display-only rotations; source DICOM orientation and measurement coordinates remain unchanged.</summary>
    public enum ViewportRotation
    {
        None = 0,
        Clockwise90 = 90,
        HalfTurn = 180,
        CounterClockwise90 = 270
    }
}