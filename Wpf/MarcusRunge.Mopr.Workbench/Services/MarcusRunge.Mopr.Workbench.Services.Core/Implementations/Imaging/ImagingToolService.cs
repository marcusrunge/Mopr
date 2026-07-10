using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Contracts.Enums;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Implementations.Imaging
{
    internal sealed class ImagingToolService : CreateableBindableBase<IImagingToolService, ImagingToolService, IImagingServiceBase>, IImagingToolService
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

        protected override void OnCreate(IImagingServiceBase @base)
        {
        }

        protected override Task OnCreateAsync(IImagingServiceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}