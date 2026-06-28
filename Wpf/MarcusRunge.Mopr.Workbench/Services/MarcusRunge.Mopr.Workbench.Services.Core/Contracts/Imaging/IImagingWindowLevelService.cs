using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public interface IImagingWindowLevelService
    {
        event EventHandler<ImagingWindowLevelChangedEventArgs>? WindowLevelChanged;

        void ResetWindowLevelToDefault();

        void SetWindowLevel(double windowCenter, double windowWidth);
    }
}