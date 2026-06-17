using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging
{
    public interface IImagingLayoutService
    {
        event EventHandler<ImagingLayoutChangedEventArgs>? CurrentLayoutChanged;

        ImagingLayout CurrentLayout { get; }

        void CycleNextLayout();

        void ResetLayout();

        void SetLayout(ImagingLayout layout);
    }
}