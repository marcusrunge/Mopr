using MarcusRunge.Mopr.Workbench.Contracts.Models;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public interface IImagingViewportService
    {
        event EventHandler<ImagingViewportStateChangedEventArgs>? StateChanged;

        ImagingViewportState State { get; }

        void MoveSlice(int delta);

        void Reset();

        void SetSlice(int currentSlice, int sliceCount);

        void SetWindowLevel(double windowValue, double levelValue);

        void SetZoom(double zoomFactor);
    }
}