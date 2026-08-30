using MarcusRunge.Mopr.Workbench.Contracts.Models;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public sealed class ImagingViewportStateChangedEventArgs(ImagingViewportState state) : EventArgs
    {
        public ImagingViewportState State { get; } = state;
    }
}