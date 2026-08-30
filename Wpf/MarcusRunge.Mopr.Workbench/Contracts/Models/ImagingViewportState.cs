namespace MarcusRunge.Mopr.Workbench.Contracts.Models
{
    public sealed class ImagingViewportState(int currentSlice, int sliceCount, double zoomFactor, double windowValue, double levelValue)
    {
        public int CurrentSlice { get; } = currentSlice;
        public double LevelValue { get; } = levelValue;
        public int SliceCount { get; } = sliceCount;
        public double WindowValue { get; } = windowValue;
        public double ZoomFactor { get; } = zoomFactor;
    }
}