using System;

namespace MarcusRunge.Mopr.Workbench.Services.Core.Contracts.Imaging
{
    public sealed class ImagingWindowLevelChangedEventArgs(double? windowCenter, double? windowWidth, bool isReset) : EventArgs
    {
        public bool IsReset { get; } = isReset;
        public double? WindowCenter { get; } = windowCenter;

        public double? WindowWidth { get; } = windowWidth;
    }
}