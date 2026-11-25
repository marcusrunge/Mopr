using System.Windows.Media;

namespace MarcusRunge.Mopr.Workbench.Modules.Imaging.Infrastructure.Viewports
{
    internal readonly record struct ViewportImagePlacement(double Width, double Height, double ImageWidth, double ImageHeight, double OffsetX, double OffsetY, Matrix ImageTransform)
    {
        public bool IsValid => double.IsFinite(Width) && double.IsFinite(Height) && double.IsFinite(ImageWidth) && double.IsFinite(ImageHeight) && double.IsFinite(OffsetX) && double.IsFinite(OffsetY) && Width > 0 && Height > 0 && ImageWidth > 0 && ImageHeight > 0;
    }
}