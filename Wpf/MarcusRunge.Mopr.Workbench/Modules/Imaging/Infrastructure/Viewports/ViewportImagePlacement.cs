namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports
{
    internal readonly record struct ViewportImagePlacement(double Width, double Height, double OffsetX, double OffsetY)
    {
        public bool IsValid => double.IsFinite(Width) && double.IsFinite(Height) && double.IsFinite(OffsetX) && double.IsFinite(OffsetY) && Width > 0 && Height > 0;
    }
}