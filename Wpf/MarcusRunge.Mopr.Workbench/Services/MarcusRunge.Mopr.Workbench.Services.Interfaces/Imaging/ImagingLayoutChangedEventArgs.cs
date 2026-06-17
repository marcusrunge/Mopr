using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging
{
    public sealed class ImagingLayoutChangedEventArgs : EventArgs
    {
        public ImagingLayoutChangedEventArgs(ImagingLayout oldLayout, ImagingLayout newLayout)
        {
            OldLayout = oldLayout;
            NewLayout = newLayout;
        }

        public ImagingLayout NewLayout { get; }
        public ImagingLayout OldLayout { get; }
    }
}