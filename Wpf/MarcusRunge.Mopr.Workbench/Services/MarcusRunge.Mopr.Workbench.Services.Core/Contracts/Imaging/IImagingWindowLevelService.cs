using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public interface IImagingWindowLevelService
    {
        event EventHandler<ImagingWindowLevelChangedEventArgs>? WindowLevelChanged;

        void ResetWindowLevel();

        void SetWindowLevel(double windowCenter, double windowWidth);
    }
}