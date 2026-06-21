using MarcusRunge.Mopr.Workbench.Contracts.Imaging;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public interface IImagingToolService
    {
        event EventHandler<ImagingToolChangedEventArgs>? ActiveToolChanged;

        ImagingTool ActiveTool { get; }

        void ClearActiveTool();

        void SetActiveTool(ImagingTool tool);
    }
}