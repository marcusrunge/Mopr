namespace MarcusRunge.Mopr.Workbench.Contracts.Models
{
    public sealed class ImagingViewportState
    {
        public ImagingViewportState(int currentSlice, int sliceCount, double zoomFactor, double windowValue, double levelValue)
        {
            CurrentSlice = currentSlice;
            SliceCount = sliceCount;
            ZoomFactor = zoomFactor;
            WindowValue = windowValue;
            LevelValue = levelValue;
        }

        public int CurrentSlice { get; }
        public double LevelValue { get; }
        public int SliceCount { get; }
        public double WindowValue { get; }
        public double ZoomFactor { get; }
    }
}