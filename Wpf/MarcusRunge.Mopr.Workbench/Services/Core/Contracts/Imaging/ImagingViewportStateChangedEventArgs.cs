using MarcusRunge.Mopr.Workbench.Contracts.Models;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public sealed class ImagingViewportStateChangedEventArgs : EventArgs
    {
        public ImagingViewportStateChangedEventArgs(ImagingViewportState state) => State = state;

        public ImagingViewportState State { get; }
    }
}