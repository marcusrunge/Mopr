using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
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