using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public sealed class ImagingLayoutChangedEventArgs(ImagingLayout oldLayout, ImagingLayout newLayout) : EventArgs
    {
        public ImagingLayout NewLayout { get; } = newLayout;
        public ImagingLayout OldLayout { get; } = oldLayout;
    }
}