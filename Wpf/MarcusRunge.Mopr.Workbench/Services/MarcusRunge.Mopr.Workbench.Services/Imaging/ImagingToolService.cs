using MarcusRunge.Mopr.Workbench.Contracts.Imaging;
using MarcusRunge.Mopr.Workbench.Services.Interfaces.Imaging;
using System;

namespace MarcusRunge.Mopr.Workbench.Services.Imaging
{
    public sealed class ImagingToolService : IImagingToolService
    {
        private ImagingTool _activeTool = ImagingTool.None;

        public event EventHandler<ImagingToolChangedEventArgs>? ActiveToolChanged;

        public ImagingTool ActiveTool => _activeTool;

        public void ClearActiveTool() => SetActiveTool(ImagingTool.None);

        public void SetActiveTool(ImagingTool tool)
        {
            if (_activeTool == tool)
            {
                return;
            }

            var oldTool = _activeTool;
            _activeTool = tool;

            ActiveToolChanged?.Invoke(this, new ImagingToolChangedEventArgs(oldTool, _activeTool));
        }
    }
}