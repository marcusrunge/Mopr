using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging
{
    public interface IImagingToolService
    {
        event EventHandler<ImagingToolChangedEventArgs>? ActiveToolChanged;

        ImagingTool ActiveTool { get; }

        void ClearActiveTool();

        void SetActiveTool(ImagingTool tool);
    }
}