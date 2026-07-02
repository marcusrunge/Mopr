namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.ViewModels
{
    public sealed class ViewportMeasurementPointInfo(string viewportId, double imageX, double imageY)
    {
        public double ImageX { get; } = imageX;
        public double ImageY { get; } = imageY;
        public string ViewportId { get; } = viewportId;
    }
}