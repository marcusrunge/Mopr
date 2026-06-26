using MarcusRunge.Base;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts;
using MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Implementations.Imaging
{
    internal sealed class ImagingWindowLevelService : CreateableBindableBase<IImagingWindowLevelService, ImagingWindowLevelService, IImagingServiceBase>, IImagingWindowLevelService
    {
        public event EventHandler<ImagingWindowLevelChangedEventArgs>? WindowLevelChanged;

        public void ResetWindowLevel() => WindowLevelChanged?.Invoke(this, new ImagingWindowLevelChangedEventArgs(windowCenter: null, windowWidth: null, isReset: true));

        public void SetWindowLevel(double windowCenter, double windowWidth) => WindowLevelChanged?.Invoke(this, new ImagingWindowLevelChangedEventArgs(windowCenter, windowWidth, isReset: false));

        protected override void OnCreate(IImagingServiceBase @base)
        {
        }

        protected override Task OnCreateAsync(IImagingServiceBase @base, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}