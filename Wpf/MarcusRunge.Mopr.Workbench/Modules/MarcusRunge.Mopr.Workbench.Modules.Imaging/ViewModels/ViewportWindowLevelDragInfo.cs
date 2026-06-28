namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ViewportWindowLevelDragInfo(string viewportId, double totalDeltaX, double totalDeltaY)
    {
        public double TotalDeltaX { get; } = totalDeltaX;
        public double TotalDeltaY { get; } = totalDeltaY;
        public string ViewportId { get; } = viewportId;
    }
}