using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public sealed class ImagingWindowLevelChangedEventArgs : EventArgs
    {
        public ImagingWindowLevelChangedEventArgs(double? windowCenter, double? windowWidth, bool isReset)
        {
            WindowCenter = windowCenter;
            WindowWidth = windowWidth;
            IsReset = isReset;
        }

        public bool IsReset { get; }
        public double? WindowCenter { get; }

        public double? WindowWidth { get; }
    }
}