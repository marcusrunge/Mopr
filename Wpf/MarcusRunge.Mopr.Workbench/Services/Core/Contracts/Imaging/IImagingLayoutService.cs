using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Contracts.Models;
using System;
using System.Collections.Generic;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public interface IImagingLayoutService
    {
        event EventHandler<ImagingLayoutChangedEventArgs>? CurrentLayoutChanged;

        ImagingLayout CurrentLayout { get; }

        void CycleNextLayout();

        IReadOnlyList<ViewportDescriptor> GetViewportsForLayout(ImagingLayout layout);

        void SetLayout(ImagingLayout layout);
    }
}